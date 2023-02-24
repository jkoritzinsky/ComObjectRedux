// Types for the COM source generator that implement the COM-specific interactions.
// All of these APIs need to be exposed to implement the COM source generator in one form or another.

using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling;

public interface IIUnknownInterfaceType
{
    /// <summary>
    /// Get a pointer to the virtual method table of managed implementations of the unmanaged interface type.
    /// </summary>
    /// <returns>A pointer to the virtual method table of managed implementations of the unmanaged interface type</returns>
    /// <remarks>
    /// Implementation will be provided by a source generator if not explicitly implemented.
    /// This property can return <c>null</c>. If it does, then the interface is not supported for passing managed implementations to unmanaged code.
    /// </remarks>
    abstract static unsafe void** ManagedVirtualMethodTable { get; }

    abstract static Guid Iid { get; }
}

/// <summary>
/// Details for the IUnknown derived interface.
/// </summary>
public interface IIUnknownDerivedDetails
{
    /// <summary>
    /// Interface ID.
    /// </summary>
    Guid Iid { get; }

    /// <summary>
    /// Managed type used to project the IUnknown derived interface.
    /// </summary>
    Type Implementation { get; }

    /// <summary>
    /// A pointer to the virtual method table to enable unmanaged callers to call a managed implementation of the interface.
    /// </summary>
    unsafe void** ManagedVirtualMethodTable { get; }

    internal static IIUnknownDerivedDetails? GetFromAttribute(RuntimeTypeHandle handle)
    {
        var type = Type.GetTypeFromHandle(handle);
        if (type is null)
        {
            return null;
        }
        return (IIUnknownDerivedDetails?)type.GetCustomAttribute(typeof(IUnknownDerivedAttribute<,>));
    }
}

[AttributeUsage(AttributeTargets.Interface)]
public sealed class IUnknownDerivedAttribute<T, TImpl> : Attribute, IIUnknownDerivedDetails
    where T : IIUnknownInterfaceType
{
    public IUnknownDerivedAttribute()
    {
    }

    /// <inheritdoc />
    public Guid Iid => T.Iid;

    /// <inheritdoc />
    public Type Implementation => typeof(TImpl);

    /// <inheritdoc />
    public unsafe void** ManagedVirtualMethodTable => T.ManagedVirtualMethodTable;
}

/// <summary>
/// IUnknown interaction strategy.
/// </summary>
public unsafe interface IIUnknownStrategy
{
    /// <summary>
    /// Create an instance pointer that represents the provided IUnknown instance.
    /// </summary>
    /// <param name="instance">The IUnknown instance.</param>
    /// <returns>A pointer representing the unmanaged instance.</returns>
    /// <remarks>
    /// This method is used to create an instance pointer that can be used to interact with the other members of this interface.
    /// For example, this method can return an IAgileReference instance for the provided IUnknown instance
    /// that can be used in the QueryInterface and Release methods to enable creating thread-local instance pointers to us
    /// through the IAgileReference APIs instead of directly calling QueryInterface on the IUnknown.
    /// </remarks>
    public void* CreateInstancePointer(void* instance);

    /// <summary>
    /// Perform a QueryInterface() for an IID on the unmanaged instance.
    /// </summary>
    /// <param name="instance">A pointer representing the unmanaged instance.</param>
    /// <param name="interfaceId">The IID (Interface ID) to query for.</param>
    /// <param name="obj">The resulting interface</param>
    /// <returns>Returns an HRESULT represents the success of the operation</returns>
    /// <seealso cref="Marshal.QueryInterface(nint, ref Guid, out nint)"/>
    public int QueryInterface(void* instance, Guid interfaceId, out void* obj);

    /// <summary>
    /// Perform a Release() call on the supplied unmanaged instance.
    /// </summary>
    /// <param name="instance">A pointer representing the unmanaged instance.</param>
    /// <returns>The current reference count.</returns>
    /// <seealso cref="Marshal.Release(nint)"/>
    public uint Release(void* instance);
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
    IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type);
}

/// <summary>
/// Unmanaged virtual method table look up strategy.
/// </summary>
public unsafe interface IIUnknownCacheStrategy
{
    public readonly struct TableInfo
    {
        public void* Instance { get; init; }
        public void** VirtualMethodTable { get; init; }
        public RuntimeTypeHandle Implementation { get; init; }
    }

    /// <summary>
    /// Construct a <see cref="TableInfo"/> instance.
    /// </summary>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <param name="ptr">Pointer to the instance to query</param>
    /// <param name="info">A <see cref="TableInfo"/> instance</param>
    /// <returns>True if success, otherwise false.</returns>
    TableInfo ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails interfaceDetails, void* ptr);

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
public sealed unsafe class ComObject : IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider
{
    private readonly void* _instancePointer;

    /// <summary>
    /// Initialize ComObject instance.
    /// </summary>
    /// <param name="interfaceDetailsStrategy">Strategy for getting details</param>
    /// <param name="iunknownStrategy">Interaction strategy for IUnknown</param>
    /// <param name="cacheStrategy">Caching strategy</param>
    /// <param name="thisPointer">Pointer to the identity IUnknown interface for the object.</param>
    internal ComObject(IIUnknownInterfaceDetailsStrategy interfaceDetailsStrategy, IIUnknownStrategy iunknownStrategy, IIUnknownCacheStrategy cacheStrategy, void* thisPointer)
    {
        InterfaceDetailsStrategy = interfaceDetailsStrategy;
        IUnknownStrategy = iunknownStrategy;
        CacheStrategy = cacheStrategy;
        _instancePointer = IUnknownStrategy.CreateInstancePointer(thisPointer);
    }

    ~ComObject()
    {
        CacheStrategy.Clear(IUnknownStrategy);
        IUnknownStrategy.Release(_instancePointer);
    }

    /// <summary>
    /// Interface details strategy.
    /// </summary>
    private IIUnknownInterfaceDetailsStrategy InterfaceDetailsStrategy { get; }

    /// <summary>
    /// IUnknown interaction strategy.
    /// </summary>
    private IIUnknownStrategy IUnknownStrategy { get; }

    /// <summary>
    /// Caching strategy.
    /// </summary>
    private IIUnknownCacheStrategy CacheStrategy { get; }

    /// <summary>
    /// Returns an IDisposable that can be used to perform a final release
    /// on this COM object wrapper.
    /// </summary>
    /// <remarks>
    /// This property will only be non-null if the ComObject was created using
    /// CreateObjectFlags.UniqueInstance.
    /// </remarks>
    public void FinalRelease()
    {
        CacheStrategy.Clear(IUnknownStrategy);
        IUnknownStrategy.Release(_instancePointer);
    }

    /// <inheritdoc />
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        if (!LookUpVTableInfo(interfaceType, out IIUnknownCacheStrategy.TableInfo info, out int qiResult))
        {
            Marshal.ThrowExceptionForHR(qiResult);
        }
        return info.Implementation;
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
            IIUnknownDerivedDetails? details = InterfaceDetailsStrategy.GetIUnknownDerivedDetails(handle);
            if (details is null)
            {
                return false;
            }
            int hr = IUnknownStrategy.QueryInterface(_instancePointer, details.Iid, out void* ppv);
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

        return new(result.Instance, result.VirtualMethodTable);
    }
}

[AttributeUsage(AttributeTargets.Interface)]
public sealed class GeneratedComInterfaceAttribute : Attribute
{
}

public abstract class StrategyBasedComWrappers : ComWrappers
{
    public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

    public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

    protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

    protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy() => DefaultIUnknownInterfaceDetailsStrategy;

    protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

    protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();

    protected override sealed unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        if (flags.HasFlag(CreateObjectFlags.TrackerObject)
            || flags.HasFlag(CreateObjectFlags.Aggregation))
        {
            throw new NotSupportedException();
        }

        var rcw = new ComObject(GetOrCreateInterfaceDetailsStrategy(), GetOrCreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject);
        if (flags.HasFlag(CreateObjectFlags.UniqueInstance))
        {
            // Set value on MyComObject to enable the FinalRelease option.
            // This could also be achieved through an internal factory
            // function on ComObject type.
        }
        return rcw;
    }

    protected override sealed void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GeneratedComClassAttribute : Attribute
{
    public GeneratedComClassAttribute(Type classType)
    {
        ClassType = classType;
    }

    public Type ClassType { get; }
}