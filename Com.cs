using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling;

public interface IIUnknownInterfaceType
{
    public abstract static Guid Iid { get; }
}

/// <summary>
/// Details for the IUnknown derived interface.
/// </summary>
public interface IUnknownDerivedDetails
{
    /// <summary>
    /// Interface ID.
    /// </summary>
    public Guid Iid { get; }

    /// <summary>
    /// Managed typed used to project the IUnknown derived interface.
    /// </summary>
    public Type Implementation { get; }

    internal static IUnknownDerivedDetails? GetFromAttribute(RuntimeTypeHandle handle)
    {
        var type = Type.GetTypeFromHandle(handle);
        if (type is null)
        {
            return null;
        }
        return (IUnknownDerivedDetails?)type.GetCustomAttribute(typeof(IUnknownDerivedAttribute<,>));
    }
}

public sealed unsafe class ComWrappersWrapperFactory<T> : IUnmanagedObjectWrapperFactory
    where T : ComWrappers, new()
{
    private static readonly T _comWrappers = new T();

    public static void* GetUnmanagedWrapperForObject(object obj)
    {
        return (void*)_comWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
    }

    public static object GetObjectForUnmanagedWrapper(void* ptr)
    {
        return _comWrappers.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);
    }
}

[AttributeUsage(AttributeTargets.Interface)]
public class IUnknownDerivedAttribute<T, TImpl> : Attribute, IUnknownDerivedDetails
    where T : IIUnknownInterfaceType
    where TImpl : T
{
    public IUnknownDerivedAttribute()
    {
    }

    /// <inheritdoc />
    public Guid Iid => T.Iid;

    /// <inheritdoc />
    public Type Implementation => typeof(TImpl);
}

/// <summary>
/// IUnknown interaction strategy.
/// </summary>
public unsafe interface IIUnknownStrategy
{
    /// <summary>
    /// Perform a QueryInterface() for an IID on the unmanaged IUnknown.
    /// </summary>
    /// <param name="thisPtr">The IUnknown instance.</param>
    /// <param name="iid">The IID (Interface ID) to query for.</param>
    /// <param name="ppObj">The resulting interface</param>
    /// <returns>Returns an HRESULT represents the success of the operation</returns>
    /// <seealso cref="Marshal.QueryInterface(nint, ref Guid, out nint)"/>
    public int QueryInterface(void* thisPtr, in Guid iid, out void* ppObj);

    /// <summary>
    /// Perform a Release() call on the supplied IUnknown instance.
    /// </summary>
    /// <param name="thisPtr">The IUnknown instance.</param>
    /// <returns>The current reference count.</returns>
    /// <seealso cref="Marshal.Release(nint)"/>
    public int Release(void* thisPtr);
}

/// <summary>
/// Strategy for acquiring interface details.
/// </summary>
public interface IIUnknownInterfaceDetailsStrategy
{
    /// <summary>
    /// Given a <see cref="RuntimeTypeHandle"/> get the IUnknown details.
    /// </summary>
    /// <param name="type">RuntimeTypeHandle instance</param>
    /// <returns>Details if type is known.</returns>
    IUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type);
}

/// <summary>
/// Unmanaged virtual method table look up strategy.
/// </summary>
public unsafe interface IIUnknownCacheStrategy
{
    public readonly struct TableInfo
    {
        public void* ThisPtr { get; init; }
        public void** Table { get; init; }
        public RuntimeTypeHandle ManagedType { get; init; }
    }

    /// <summary>
    /// Construct a <see cref="TableInfo"/> instance.
    /// </summary>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <param name="ptr">Pointer to the instance to query</param>
    /// <param name="info">A <see cref="TableInfo"/> instance</param>
    /// <returns>True if success, otherwise false.</returns>
    TableInfo ConstructTableInfo(RuntimeTypeHandle handle, IUnknownDerivedDetails interfaceDetails, void* ptr);

    /// <summary>
    /// Get associated <see cref="TableInfo"/>.
    /// </summary>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <param name="info">A <see cref="TableInfo"/> instance</param>
    /// <returns>True if found, otherwise false.</returns>
    bool TryGetTableInfo(RuntimeTypeHandle handle, out TableInfo info);

    /// <summary>
    /// Set associated <see cref="TableInfo"/>.
    /// </summary>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <param name="info">A <see cref="TableInfo"/> instance</param>
    /// <returns>True if set, otherwise false.</returns>
    bool TrySetTableInfo(RuntimeTypeHandle handle, TableInfo info);

    /// <summary>
    /// Clear the cache
    /// </summary>
    /// <param name="unknownStrategy">The <see cref="IIUnknownStrategy"/> to use for clearing</param>
    void Clear(IIUnknownStrategy unknownStrategy);
}

/// <summary>
/// Base class for all COM source generated Runtime Callable Wrapper (RCWs).
/// </summary>
public abstract unsafe class ComObject : IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider
{
    /// <summary>
    /// Initialize ComObject instance.
    /// </summary>
    /// <param name="interfaceDetailsStrategy">Strategy for getting details</param>
    /// <param name="iunknownStrategy">Interaction strategy for IUnknown</param>
    /// <param name="cacheStrategy">Caching strategy</param>
    protected ComObject(IIUnknownInterfaceDetailsStrategy interfaceDetailsStrategy, IIUnknownStrategy iunknownStrategy, IIUnknownCacheStrategy cacheStrategy)
    {
        InterfaceDetailsStrategy = interfaceDetailsStrategy;
        IUnknownStrategy = iunknownStrategy;
        CacheStrategy = cacheStrategy;
    }

    ~ComObject()
    {
        CacheStrategy.Clear(IUnknownStrategy);
        IUnknownStrategy.Release(ThisPtr);
    }

    /// <summary>
    /// Pointer to the unmanaged instance.
    /// </summary>
    protected void* ThisPtr { get; init; }

    /// <summary>
    /// Interface details strategy.
    /// </summary>
    protected IIUnknownInterfaceDetailsStrategy InterfaceDetailsStrategy { get; init; }

    /// <summary>
    /// IUnknown interaction strategy.
    /// </summary>
    protected IIUnknownStrategy IUnknownStrategy { get; init; }

    /// <summary>
    /// Caching strategy.
    /// </summary>
    protected IIUnknownCacheStrategy CacheStrategy { get; init; }

    /// <summary>
    /// Returns an IDisposable that can be used to perform a final release
    /// on this COM object wrapper.
    /// </summary>
    /// <remarks>
    /// This property will only be non-null if the ComObject was created using
    /// CreateObjectFlags.UniqueInstance.
    /// </remarks>
    public IDisposable? FinalRelease { get; }

    /// <inheritdoc />
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        if (!LookUpVTableInfo(interfaceType, out IIUnknownCacheStrategy.TableInfo info, out int qiResult))
        {
            Marshal.ThrowExceptionForHR(qiResult);
        }
        return info.ManagedType;
    }

    /// <inheritdoc />
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        if (!LookUpVTableInfo(interfaceType, out _, out int qiResult))
        {
            if (throwIfNotImplemented)
            {
                Marshal.ThrowExceptionForHR(qiResult);
            }
            return false;
        }
        return true;
    }

    private bool LookUpVTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo result, out int qiHResult)
    {
        qiHResult = 0;
        if (!CacheStrategy.TryGetTableInfo(handle, out result))
        {
            IUnknownDerivedDetails? details = InterfaceDetailsStrategy.GetIUnknownDerivedDetails(handle);
            if (details is null)
            {
                return false;
            }
            int hr = IUnknownStrategy.QueryInterface(ThisPtr, details.Iid, out void* ppv);
            if (hr < 0)
            {
                qiHResult = hr;
                return false;
            }

            result = CacheStrategy.ConstructTableInfo(handle, details, ppv);

            // Update some local cache. If the update fails, we lost the race and
            // then are responsible for calling Release().
            if (!CacheStrategy.TrySetTableInfo(handle, result))
            {
                bool found = CacheStrategy.TryGetTableInfo(handle, out result);
                Debug.Assert(found);
                _ = IUnknownStrategy.Release(ppv);
            }
        }

        return true;
    }

    /// <inheritdoc />
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        if (!LookUpVTableInfo(type.TypeHandle, out IIUnknownCacheStrategy.TableInfo result, out int qiHResult))
        {
            Marshal.ThrowExceptionForHR(qiHResult);
        }

        return new(result.ThisPtr, result.Table);
    }
}

public abstract class GeneratedComWrappersBase<TComObject> : ComWrappers
{
    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}