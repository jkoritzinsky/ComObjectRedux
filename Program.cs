using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#region Base impl
public readonly ref struct VirtualMethodTableInfo
{
    public VirtualMethodTableInfo(IntPtr thisPointer, ReadOnlySpan<IntPtr> virtualMethodTable)
    {
        ThisPointer = thisPointer;
        VirtualMethodTable = virtualMethodTable;
    }

    public IntPtr ThisPointer { get; }
    public ReadOnlySpan<IntPtr> VirtualMethodTable { get; }

    public void Deconstruct(out IntPtr thisPointer, out ReadOnlySpan<IntPtr> virtualMethodTable)
    {
        thisPointer = ThisPointer;
        virtualMethodTable = VirtualMethodTable;
    }
}

public interface IUnmanagedVirtualMethodTableProvider
{
    // Forward from a generic non-virtual to a non-generic virtual as lookup
    // for interface information can go through generic attributes like we do below for COM
    // for whatever information is required for the domain-specific implementation
    // and generic virtual method dispatch can be extremely slow.
    protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type t);

    public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
        where TUnmanagedInterfaceType : IUnmanagedInterfaceType
    {
        return GetVirtualMethodTableInfoForKey(typeof(TUnmanagedInterfaceType));
    }
}

public interface IUnmanagedInterfaceType
{
    public abstract static int VTableLength { get; }
}

public interface IComInterfaceType
{
    public abstract static Guid Iid { get; }
}
#endregion Base impl

#region COM layer
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

    /// <summary>
    /// Total length of the vtable.
    /// </summary>
    public int VTableTotalLength { get; }

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

/// <summary>
/// Attribute used to indicate an interface derives from IUnknown.
/// </summary>
/// <typeparam name="T">The managed definition of the derived interface.</typeparam>
/// <typeparam name="TImpl">The managed implementation of the derived interface.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public class IUnknownDerivedAttribute<T, TImpl> : Attribute, IUnknownDerivedDetails
    where T : IUnmanagedInterfaceType, IComInterfaceType
    where TImpl : T
{
    public IUnknownDerivedAttribute()
    {
    }

    /// <inheritdoc />
    public Guid Iid => T.Iid;

    /// <inheritdoc />
    public Type Implementation => typeof(TImpl);

    /// <inheritdoc />
    public int VTableTotalLength => T.VTableLength;
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
        public int TableLength { get; init; }
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

        return new((nint)result.ThisPtr, new ReadOnlySpan<nint>(result.Table, result.TableLength));
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
            TableLength = details.VTableTotalLength,
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

internal sealed class MyGeneratedComWrappers : GeneratedComWrappersBase<MyComObjectBase>
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
        return flags.HasFlag(CreateObjectFlags.UniqueInstance)
            ? new MyDisposableComObject((void*)externalComObject)
            : new MyComObject((void*)externalComObject);
    }
}

// Minimal implementation for all IUnknown based scenarios.
internal sealed unsafe class MyComObject : MyComObjectBase
{
    internal MyComObject(void* thisPtr)
        : base(thisPtr)
    {
    }
}

// Minimal implementation for all "unique instance" scenarios,
// that are capable of supporting the IDisposable pattern.
internal sealed unsafe class MyDisposableComObject : MyComObjectBase, IDisposable
{
    private bool _isDisposed = false;

    internal MyDisposableComObject(void* thisPtr)
        : base(thisPtr)
    { }

    void IDisposable.Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        CacheStrategy.Clear(IUnknownStrategy);
        IUnknownStrategy.Release(ThisPtr);
        GC.SuppressFinalize(this);
        _isDisposed = true;
    }
}

[IUnknownDerived<IComInterface1, Impl>]
public partial interface IComInterface1 : IUnmanagedInterfaceType, IComInterfaceType
{
    static Guid IComInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b1");
    static int IUnmanagedInterfaceType.VTableLength => 4;

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface1
    {
        void IComInterface1.Method()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IComInterface1>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[3])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}

[IUnknownDerived<IComInterface2, Impl>]
public partial interface IComInterface2 : IUnmanagedInterfaceType, IComInterfaceType
{
    static Guid IComInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b2");
    static int IUnmanagedInterfaceType.VTableLength => 5;

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface2
    {
        void IComInterface2.Method1()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IComInterface2>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[3])(thisPtr);
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
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IComInterface2>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[4])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}

[IUnknownDerived<IComInterface3, Impl>]
public partial interface IComInterface3 : IUnmanagedInterfaceType, IComInterfaceType
{
    static Guid IComInterfaceType.Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b3");

    static int IUnmanagedInterfaceType.VTableLength => 4;

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface3
    {
        void IComInterface3.Method()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey<IComInterface3>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[3])(thisPtr);
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
internal unsafe class MyComObjectBase : ComObject
{
    internal MyComObjectBase(void* thisPtr)
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
            where T : IComInterfaceType
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
