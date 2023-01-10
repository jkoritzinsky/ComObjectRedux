using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#region Base impl
namespace System.Runtime.InteropServices.Marshalling;

/// <summary>
/// Information about a virtual method table and the unmanaged instance pointer.
/// </summary>
public readonly unsafe struct VirtualMethodTableInfo
{
    /// <summary>
    /// Construct a <see cref="VirtualMethodTableInfo"/> from a given instance pointer and table memory.
    /// </summary>
    /// <param name="thisPointer">The pointer to the instance.</param>
    /// <param name="virtualMethodTable">The block of memory that represents the virtual method table.</param>
    public VirtualMethodTableInfo(void* thisPointer, void** virtualMethodTable)
    {
        ThisPointer = thisPointer;
        VirtualMethodTable = virtualMethodTable;
    }

    /// <summary>
    /// The unmanaged instance pointer
    /// </summary>
    public void* ThisPointer { get; }

    /// <summary>
    /// The virtual method table.
    /// </summary>
    public void** VirtualMethodTable { get; }

    /// <summary>
    /// Deconstruct this structure into its two fields.
    /// </summary>
    /// <param name="thisPointer">The <see cref="ThisPointer"/> result</param>
    /// <param name="virtualMethodTable">The <see cref="VirtualMethodTable"/> result</param>
    public void Deconstruct(out void* thisPointer, out void** virtualMethodTable)
    {
        thisPointer = ThisPointer;
        virtualMethodTable = VirtualMethodTable;
    }
}

/// <summary>
/// This interface allows an object to provide information about a virtual method table for a managed interface to enable invoking methods in the virtual method table.
/// </summary>
public unsafe interface IUnmanagedVirtualMethodTableProvider
{
    /// <summary>
    /// Get the information about the virtual method table for a given unmanaged interface type represented by <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The managed type for the unmanaged interface.</param>
    /// <returns>The virtual method table information for the unmanaged interface.</returns>
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);
}

/// <summary>
/// A factory to create an unmanaged "this pointer" from a managed object and to get a managed object from an unmanaged "this pointer".
/// </summary>
public unsafe interface IUnmanagedObjectWrapperFactory
{
    /// <summary>
    /// Get a pointer that wraps a managed implementation of an unmanaged interface that can be passed to unmanaged code.
    /// </summary>
    /// <param name="obj">The managed object that implements the unmanaged interface.</param>
    /// <returns>A unmanaged "this pointer" that can be passed to unmanaged code that represents <paramref name="obj"/></returns>
    public static abstract void* GetUnmanagedWrapperForObject(object obj);

    /// <summary>
    /// Get the object wrapped by <paramref name="ptr"/>.
    /// </summary>
    /// <param name="ptr">A an unmanaged "this pointer".</param>
    /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
    public static abstract object GetObjectForUnmanagedWrapper(void* ptr);
}

/// <summary>
/// This interface allows another interface to define that it represents a manavged projection of an unmanaged interface from some unmanaged type system and supports passing managed implementations of unmanaged interfaces to unmanaged code.
/// </summary>
/// <typeparam name="TInterface">The managed interface.</typeparam>
/// <typeparam name="TUnmanagedObjectWrapperFactory">The factory to create an unmanaged "this pointer" from a managed object and to get a managed object from an unmanaged "this pointer".</typeparam>
public unsafe interface IUnmanagedInterfaceType<TInterface, TUnmanagedObjectWrapperFactory>
    where TInterface : IUnmanagedInterfaceType<TInterface, TUnmanagedObjectWrapperFactory>
    where TUnmanagedObjectWrapperFactory: IUnmanagedObjectWrapperFactory, new()
{
    /// <summary>
    /// Get a pointer to the virtual method table of managed implementations of the unmanaged interface type.
    /// </summary>
    /// <returns>A pointer to the virtual method table of managed implementations of the unmanaged interface type</returns>
    /// <remarks>TODO: Source generated</remarks>
    public abstract static void* VirtualMethodTableManagedImplementation { get; }

    /// <summary>
    /// Get a pointer that wraps a managed implementation of an unmanaged interface that can be passed to unmanaged code.
    /// </summary>
    /// <param name="obj">The managed object that implements the unmanaged interface.</param>
    /// <returns>A unmanaged "this pointer" that can be passed to unmanaged code that represents <paramref name="obj"/></returns>
    public static void* GetUnmanagedWrapperForObject(TInterface obj) { return TUnmanagedObjectWrapperFactory.GetUnmanagedWrapperForObject(obj); }

    /// <summary>
    /// Get the object wrapped by <paramref name="ptr"/>.
    /// </summary>
    /// <param name="ptr">A an unmanaged "this pointer".</param>
    /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
    public static TInterface GetObjectForUnmanagedWrapper(void* ptr) { return (TInterface)TUnmanagedObjectWrapperFactory.GetObjectForUnmanagedWrapper(ptr); }
}
/// <summary>
/// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The unmanaged type to convert the HResult to.</typeparam>

// TODO: Update our correctness analyzer to allow a non-generic managed type with a generic marshaller.
// We can determine the correct information at the usage site.
#pragma warning disable SYSLIB1055 // The managed type 'System.Exception' for entry-point marshaller type 'System.Runtime.InteropServices.Marshalling.ExceptionHResultMarshaller<T>' must be a closed generic type, have the same arity as the managed type if it is a value marshaller, or have one additional generic parameter if it is a collection marshaller.

[CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionHResultMarshaller<>))]
#pragma warning restore SYSLIB1055
public static class ExceptionHResultMarshaller<T>
    where T : unmanaged, INumber<T>
{
    /// <summary>
    /// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="e">The exception.</param>
    /// <returns>The HResult of the exception, converted to <typeparamref name="T"/>.</returns>
    public static T ConvertToUnmanaged(Exception e)
    {
        // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
        // and calls SetErrorInfo if the platform supports it.

        // We use CreateTruncating here to convert from the int return type of Marshal.GetHRForException
        // to whatever the T is. A "truncating" conversion in this case is the same as an unchecked conversion like
        // (uint)Marshal.GetHRForException(e) would be if we were writing a non-generic marshaller.
        // Since we're using the INumber<T> interface, this is the correct mechanism to represent that conversion.
        return T.CreateTruncating(Marshal.GetHRForException(e));
    }
}
#endregion Base impl

#region COM layer
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
#endregion COM layer

#region Common Stragety Implementations

public sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
{
    public static readonly IIUnknownInterfaceDetailsStrategy Instance = new DefaultIUnknownInterfaceDetailsStrategy();

    public IUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
    {
        return IUnknownDerivedDetails.GetFromAttribute(type);
    }
}

public sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
{
    public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

    unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
    {
        int hr = Marshal.QueryInterface((nint)thisPtr, ref Unsafe.AsRef(in handle), out nint ppv);
        if (hr < 0)
        {
            ppObj = null;
        }
        else
        {
            ppObj = (void*)ppv;
        }
        return hr;
    }

    unsafe int IIUnknownStrategy.Release(void* thisPtr)
        => Marshal.Release((nint)thisPtr);
}

public sealed unsafe class DefaultCaching : IIUnknownCacheStrategy
{
    // [TODO] Implement some smart/thread-safe caching
    private readonly Dictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new();

    IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IUnknownDerivedDetails details, void* ptr)
    {
        var obj = (void***)ptr;
        return new IIUnknownCacheStrategy.TableInfo()
        {
            ThisPtr = obj,
            Table = *obj,
            ManagedType = details.Implementation.TypeHandle
        };
    }

    bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryGetValue(handle, out info);
    }

    bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryAdd(handle, info);
    }

    void IIUnknownCacheStrategy.Clear(IIUnknownStrategy unknownStrategy)
    {
        foreach (var info in _cache.Values)
        {
            _ = unknownStrategy.Release(info.ThisPtr);
        }
        _cache.Clear();
    }
}
#endregion

#region Generated
file abstract unsafe class IUnknownVTableComWrappers : ComWrappers
{
	public static void GetIUnknownImpl(out void* pQueryInterface, out void* pAddRef, out void* pRelease)
    {
		nint qi, addRef, release;
        ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
        pQueryInterface = (void*)qi;
        pAddRef = (void*)addRef;
        pRelease = (void*)release;
    }
}

public sealed class MyGeneratedComWrappers : GeneratedComWrappersBase<MyComObject>
{
    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        => throw new NotImplementedException();

    protected override unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        if (flags.HasFlag(CreateObjectFlags.TrackerObject)
            || flags.HasFlag(CreateObjectFlags.Aggregation))
        {
            throw new NotSupportedException();
        }

        var rcw = new MyComObject((void*)externalComObject);
        if (flags.HasFlag(CreateObjectFlags.UniqueInstance))
        {
            // Set value on MyComObject to enable the FinalRelease option.
            // This could also be achieved through an internal factory
            // function on ComObject type.
        }
        return rcw;
    }
}

[IUnknownDerived<IComInterface1, Impl>]
public unsafe partial interface IComInterface1 : IUnmanagedInterfaceType<IComInterface1, ComWrappersWrapperFactory<MyGeneratedComWrappers>>, IIUnknownInterfaceType
{
    static Guid IIUnknownInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b1");

    private static void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComInterface1), sizeof(void*) * 4);

    static void* IUnmanagedInterfaceType<IComInterface1, ComWrappersWrapperFactory<MyGeneratedComWrappers>>.VirtualMethodTableManagedImplementation
    {
        get
        {
            if (m_vtable[0] == null)
            {
                IUnknownVTableComWrappers.GetIUnknownImpl(out m_vtable[0], out m_vtable[1], out m_vtable[2]);
                Impl.PopulateManagedVirtualMethodTable(m_vtable);
            }
            return m_vtable;
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface1
    {
        internal static void PopulateManagedVirtualMethodTable(void* table)
        {
            var vtable = (void**)table;
            vtable[3] = (delegate* unmanaged[Stdcall, MemberFunction]<void*, int>)&ABI_Method;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
        private static int ABI_Method(void* thisPtr)
        {
            int retVal = 0;
            try
            {
                IComInterface1 @this = GetObjectForUnmanagedWrapper(thisPtr);
                @this.Method();
            }
            catch (System.Exception ex)
            {
                retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
            }
            return retVal;
        }

        void IComInterface1.Method()
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComInterface1));
            int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}

[IUnknownDerived<IComInterface2, Impl>]
public unsafe partial interface IComInterface2 : IUnmanagedInterfaceType<IComInterface2, ComWrappersWrapperFactory<MyGeneratedComWrappers>>, IIUnknownInterfaceType
{
    static Guid IIUnknownInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b2");

    private static void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComInterface1), sizeof(void*) * 5);

    static void* IUnmanagedInterfaceType<IComInterface2, ComWrappersWrapperFactory<MyGeneratedComWrappers>>.VirtualMethodTableManagedImplementation
    {
        get
        {
            if (m_vtable[0] == null)
            {
                IUnknownVTableComWrappers.GetIUnknownImpl(out m_vtable[0], out m_vtable[1], out m_vtable[2]);
                Impl.PopulateManagedVirtualMethodTable(m_vtable);
            }
            return m_vtable;
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface2
    {
        internal static void PopulateManagedVirtualMethodTable(void* table)
        {
            var vtable = (void**)table;
            vtable[3] = (delegate* unmanaged[Stdcall, MemberFunction]<void*, int>)&ABI_Method1;
            vtable[4] = (delegate* unmanaged[Stdcall, MemberFunction]<void*, int>)&ABI_Method2;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
        private static int ABI_Method1(void* thisPtr)
        {
            int retVal = 0;
            try
            {
                IComInterface2 @this = GetObjectForUnmanagedWrapper(thisPtr);
                @this.Method1();
            }
            catch (System.Exception ex)
            {
                retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
            }
            return retVal;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
        private static int ABI_Method2(void* thisPtr)
        {
            int retVal = 0;
            try
            {
                IComInterface2 @this = GetObjectForUnmanagedWrapper(thisPtr);
                @this.Method2();
            }
            catch (System.Exception ex)
            {
                retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
            }
            return retVal;
        }

        void IComInterface2.Method1()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComInterface2));
                int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
        void IComInterface2.Method2()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComInterface2));
                int hr = ((delegate* unmanaged<void*, int>)vtable[4])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}

[IUnknownDerived<IComInterface3, Impl>]
public unsafe partial interface IComInterface3 : IUnmanagedInterfaceType<IComInterface3, ComWrappersWrapperFactory<MyGeneratedComWrappers>>, IIUnknownInterfaceType
{
    static Guid IIUnknownInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b3");


    private static void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComInterface3), sizeof(void*) * 4);

    static void* IUnmanagedInterfaceType<IComInterface3, ComWrappersWrapperFactory<MyGeneratedComWrappers>>.VirtualMethodTableManagedImplementation
    {
        get
        {
            if (m_vtable[0] == null)
            {
                IUnknownVTableComWrappers.GetIUnknownImpl(out m_vtable[0], out m_vtable[1], out m_vtable[2]);
                Impl.PopulateManagedVirtualMethodTable(m_vtable);
            }
            return m_vtable;
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface3
    {
        internal static void PopulateManagedVirtualMethodTable(void* table)
        {
            var vtable = (void**)table;
            vtable[3] = (delegate* unmanaged[Stdcall, MemberFunction]<void*, int>)&ABI_Method;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
        private static int ABI_Method(void* thisPtr)
        {
            int retVal = 0;
            try
            {
                IComInterface3 @this = GetObjectForUnmanagedWrapper(thisPtr);
                @this.Method();
            }
            catch (System.Exception ex)
            {
                retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
            }
            return retVal;
        }

        void IComInterface3.Method()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComInterface3));
                int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}
#endregion Generated

#region User defined
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface1
{
    void Method();
}

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface2
{
    void Method1();
    void Method2();
}

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface3
{
    void Method();
}

// User-defined implementation of ComObject that provides the requested strategy implementations.
// This type will be provided to the source generator through the GeneratedComInterface attribute.
public unsafe class MyComObject : ComObject
{
    internal MyComObject(void* thisPtr)
        : base(DefaultIUnknownInterfaceDetailsStrategy.Instance, FreeThreadedStrategy.Instance, new DefaultCaching())
    {
        // Implementers can, at this point, capture the current thread
        // context and create a proxy for apartment marshalling. The options
        // are to use RoGetAgileReference() on Win 8.1+ or the Global Interface Table (GIT)
        // on pre-Win 8.1.
        //
        // Relevant APIs:
        //  - RoGetAgileReference() - modern way to create apartment proxies
        //  - IGlobalInterfaceTable - GIT interface that helps with proxy management
        //  - CoGetContextToken()   - Low level mechanism for tracking object's apartment context
        //
        // Once the decision has been made to create a proxy (i.e., not free threaded) the
        // implementer should set the instance pointer.
        ThisPtr = thisPtr;
    }
}
#endregion User defined

public unsafe class Program
{
    private static void Main(string[] args)
    {
        // Activate native COM instances
        void*[] instances = ActivateNativeCOMInstances();

        // Test the instances
        Run(instances);

        // Clean up the RCWs
        GC.Collect();
        GC.WaitForPendingFinalizers();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Run(void*[] instances)
        {
            for (int i = 0; i < instances.Length; ++i)
            {
                Console.WriteLine($"=== Instance {i}");
                var rcw = new MyComObject(instances[i]); // This would be replaced with a ComWrappers implementation.
                InspectObject(rcw);
                InspectObject(rcw);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void InspectObject(object obj)
        {
            if (obj is IComInterface1 c1)
            {
                c1.Method();
            }
            if (obj is IComInterface2 c2)
            {
                c2.Method1();
                c2.Method2();
            }
            if (obj is IComInterface3 c3)
            {
                c3.Method();
            }
        }
    }

#region Unmanaged code region
    static readonly void*[] tables = new void*[3];
    static readonly void*[] impls = new void*[3];

    const nint SupportNone = 0;
    const nint SupportComInterface1 = 1;
    const nint SupportComInterface2 = 2;
    const nint SupportComInterface3 = 4;

    [UnmanagedCallersOnly]
    static int QueryInterface(void* thisPtr, Guid* iid, void** ppObj)
    {
        var inst = new ReadOnlySpan<nint>(thisPtr, 2);
        if (*iid == GetTypeKey<IComInterface1>() && (inst[1] & SupportComInterface1) != 0)
        {
            *ppObj = impls[0];
        }
        else if (*iid == GetTypeKey<IComInterface2>() && (inst[1] & SupportComInterface2) != 0)
        {
            *ppObj = impls[1];
        }
        else if (*iid == GetTypeKey<IComInterface3>() && (inst[1] & SupportComInterface3) != 0)
        {
            *ppObj = impls[2];
        }
        else
        {
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            return E_NOINTERFACE;
        }

        Console.WriteLine($"--- {nameof(QueryInterface)}");
        Marshal.AddRef((nint)thisPtr);
        return 0;

        static Guid GetTypeKey<T>()
            where T : IIUnknownInterfaceType
        {
            return T.Iid;
        }
    }

    [UnmanagedCallersOnly]
    static uint AddRef(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(AddRef)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static uint Release(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(Release)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI1_Method(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI1_Method)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI2_Method1(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI2_Method1)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI2_Method2(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI2_Method2)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI3_Method(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI3_Method)}");
        return 0;
    }

    private static void*[] ActivateNativeCOMInstances()
    {
        void** table;
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 4 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI1_Method;
            tables[0] = table;
        }
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 5 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI2_Method1;
            table[4] = (delegate* unmanaged<void*, int>)&CI2_Method2;
            tables[1] = table;
        }
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 4 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI3_Method;
            tables[2] = table;
        }

        // Build the instances
        for (int i = 0; i < impls.Length; ++i)
        {
            void** instance = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 2 * sizeof(void*));

            // Define which interfaces for each instance
            var inst = new Span<nint>(instance, 2);
            inst[1] = i switch
            {
                0 => SupportComInterface1,
                1 => SupportComInterface1 | SupportComInterface3,
                2 => SupportComInterface2,
                _ => SupportNone
            };

            // Set up the instance with the vtable
            instance[0] = tables[i];
            impls[i] = instance;
        }

        return impls;
    }
#endregion Unmanaged code region
}
