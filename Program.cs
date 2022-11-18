using System.Collections;
using System.Diagnostics;
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

public interface IUnmanagedVirtualMethodTableProvider<T> where T : IEquatable<T>
{
    protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(T typeKey);

    public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
        where TUnmanagedInterfaceType : IUnmanagedInterfaceType<T>
    {
        return GetVirtualMethodTableInfoForKey(TUnmanagedInterfaceType.TypeKey);
    }
}

public interface IUnmanagedInterfaceType<T> where T : IEquatable<T>
{
    public abstract static T TypeKey { get; }
}
#endregion Base impl

#region COM layer
public readonly record struct InterfaceId(Guid Iid);

/// <summary>
/// IUnknown interaction strategy.
/// </summary>
public unsafe interface IIUnknownStrategy
{
    /// <summary>
    /// Perform a QueryInterface() for an IID on the unmanaged IUnknown.
    /// </summary>
    /// <param name="thisPtr">The IUnknown instance.</param>
    /// <param name="handle">The IID (Interface ID) to query for.</param>
    /// <param name="ppObj">The resulting interface</param>
    /// <returns>Returns an HRESULT represents the success of the operation</returns>
    /// <seealso cref="Marshal.QueryInterface(nint, ref Guid, out nint)"/>
    public int QueryInterface(void* thisPtr, RuntimeTypeHandle handle, out void* ppObj);

    /// <summary>
    /// Perform a Release() call on the supplied IUnknown instance.
    /// </summary>
    /// <param name="thisPtr">The IUnknown instance.</param>
    /// <returns>The current reference count.</returns>
    /// <seealso cref="Marshal.Release(nint)"/>
    public int Release(void* thisPtr);
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
    /// Map an IID to a its interface's <see cref="RuntimeTypeHandle"/>.
    /// </summary>
    /// <param name="iid">Interface ID</param>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <returns>True if a mapping exists, otherwise false.</returns>
    bool TryMapIidToInterfaceHandle(Guid iid, out RuntimeTypeHandle handle);

    /// <summary>
    /// Construct a <see cref="TableInfo"/> instance.
    /// </summary>
    /// <param name="handle">RuntimeTypeHandle instance</param>
    /// <param name="ptr">Pointer to the instance to query</param>
    /// <param name="info">A <see cref="TableInfo"/> instance</param>
    /// <returns>True if success, otherwise false.</returns>
    bool TryConstructTableInfo(RuntimeTypeHandle handle, void* ptr, out TableInfo info);

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
}

public abstract unsafe class ComObject : IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider<InterfaceId>
{
    protected ComObject(IIUnknownStrategy iunknownStrategy, IIUnknownCacheStrategy cacheStrategy)
    {
        IUnknownStrategy = iunknownStrategy;
        CacheStrategy = cacheStrategy;
    }

    ~ComObject()
    {
        IUnknownStrategy.Release(ThisPtr);
    }

    protected void* ThisPtr { get; init; }
    protected IIUnknownStrategy IUnknownStrategy { get; init; }
    protected IIUnknownCacheStrategy CacheStrategy { get; init; }

    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        if (!LookUpVTableInfo(interfaceType, out IIUnknownCacheStrategy.TableInfo info, out int qiResult))
        {
            Marshal.ThrowExceptionForHR(qiResult);
        }
        return info.ManagedType;
    }

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
            int hr = IUnknownStrategy.QueryInterface(ThisPtr, handle, out void* ppv);
            if (hr < 0)
            {
                qiHResult = hr;
                return false;
            }

            if (!CacheStrategy.TryConstructTableInfo(handle, ppv, out result))
            {
                return false;
            }

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

    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider<InterfaceId>.GetVirtualMethodTableInfoForKey(InterfaceId typeKey)
    {
        Guid iid = typeKey.Iid;

        if (!CacheStrategy.TryMapIidToInterfaceHandle(iid, out RuntimeTypeHandle handle))
        {
            throw new TypeAccessException();
        }

        IIUnknownCacheStrategy.TableInfo result;
        if (!LookUpVTableInfo(handle, out result, out int qiHResult))
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
#endregion User defined

#region Generated
// Generated base for v1.0 supported runtime scenarios.
// No thread-affinity aware support.
// No IDispatch support.
// No aggregation support.
//
// This would be generated for a set of "version bubbled" COM runtime wrappers, RCWs.
internal unsafe class MyComObjectBase : ComObject
{
    internal MyComObjectBase(void* thisPtr)
        : base(FreeThreadedStrategy.Instance, new DefaultCaching())
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
        IUnknownStrategy.Release(ThisPtr);
        GC.SuppressFinalize(this);
        _isDisposed = true;
    }
}

internal sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
{
    public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

    unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, RuntimeTypeHandle handle, out void* ppObj)
    {
        if (!ComProxies.TryFindGuid(handle, out Guid iid))
        {
            ppObj = null;
            return -1; // [TODO] What is the HRESULT here?
        }

        int hr = Marshal.QueryInterface((nint)thisPtr, ref iid, out nint ppv);
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

internal sealed unsafe class DefaultCaching : IIUnknownCacheStrategy
{
    // [TODO] Implement some smart caching
    private Dictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new();

    bool IIUnknownCacheStrategy.TryConstructTableInfo(RuntimeTypeHandle handle, void* ptr, out IIUnknownCacheStrategy.TableInfo info)
    {
        if (!ComProxies.TryFindGuid(handle, out Guid iid)
            || !ComProxies.TryFindImpl(iid, out var impl))
        {
            info = default;
            return false;
        }

        var obj = (void***)ptr;
        info = new IIUnknownCacheStrategy.TableInfo()
        {
            ThisPtr = obj,
            Table = *obj,
            TableLength = impl.VTableTotalLength,
            ManagedType = impl.Impl.TypeHandle
        };

        return true;
    }

    bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryGetValue(handle, out info);
    }

    bool IIUnknownCacheStrategy.TryMapIidToInterfaceHandle(Guid iid, out RuntimeTypeHandle handle)
    {
        if (!ComProxies.TryFindImpl(iid, out var impl))
        {
            handle = default;
            return false;
        }

        handle = impl.Interface.TypeHandle;
        return true;
    }

    bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
    {
        return _cache.TryAdd(handle, info);
    }
}

internal static class ComProxies
{
    // Ordered - the first integer must be encoded in little endian form.
    //
    // Note that data in this array is not guaranteed to be aligned in a manner that makes a
    // conversion to Guid possible. Consider the case where MemoryMarshal.Cast<byte, Guid>()
    // is used to search this blob. The MemoryMarshal.Cast<> API doesn't handle unaligned data.
    // A potential mitigation here is to write a specialized binary search for this data
    // structure that uses memcmp() with bytes and avoid all memory alignment issues.
    //
    // Perhaps have different versions for alignment sensitive/non-sensitive platforms?
    public static ReadOnlySpan<byte> Iids => new byte[]
    {
        // 2c3f9903-b586-46b1-881b-adfce9af47b1
        0x03, 0x99, 0x3f, 0x2c, 0xb5, 0x86, 0x46, 0xb1, 0x88, 0x1b, 0xad, 0xfc, 0xe9, 0xaf, 0x47, 0xb1,
        // 2c3f9903-b586-46b1-881b-adfce9af47b2
        0x03, 0x99, 0x3f, 0x2c, 0xb5, 0x86, 0x46, 0xb1, 0x88, 0x1b, 0xad, 0xfc, 0xe9, 0xaf, 0x47, 0xb2,
        // 2c3f9903-b586-46b1-881b-adfce9af47b3
        0x03, 0x99, 0x3f, 0x2c, 0xb5, 0x86, 0x46, 0xb1, 0x88, 0x1b, 0xad, 0xfc, 0xe9, 0xaf, 0x47, 0xb3,
    };

    public static unsafe bool TryFindImpl(Guid guid, out (Type Interface, Type Impl, int VTableTotalLength, int VTableFirstInstance) impl)
    {
        const int elementSize = 16;
        Debug.Assert(elementSize == sizeof(Guid));

        var key = new ReadOnlySpan<byte>((byte*)&guid, elementSize);
        int count = Iids.Length / elementSize;

        int baseIdx = 0;
        while (count > 0)
        {
            var rowIdx = baseIdx + elementSize * (count / 2);
            int res = key.SequenceCompareTo(Iids.Slice(rowIdx, elementSize));
            if (res == 0)
            {
                impl = Impls[rowIdx / 16];
                return true;
            }

            if (count == 1)
            {
                break;
            }
            else if (res < 0)
            {
                count /= 2;
            }
            else
            {
                baseIdx = rowIdx;
                count -= count / 2;
            }
        }
        impl = default;
        return false;
    }

    public static bool TryFindGuid(RuntimeTypeHandle handle, out Guid iid)
    {
        Type type = Type.GetTypeFromHandle(handle)!;
        if (type == typeof(IComInterface1))
        {
            iid = GetTypeKey<IComInterface1>();
        }
        else if (type == typeof(IComInterface2))
        {
            iid = GetTypeKey<IComInterface2>();
        }
        else if (type == typeof(IComInterface3))
        {
            iid = GetTypeKey<IComInterface3>();
        }
        else
        {
            iid = default;
            return false;
        }

        return true;

        static Guid GetTypeKey<T>()
            where T : IUnmanagedInterfaceType<InterfaceId>
        {
            return T.TypeKey.Iid;
        }
    }

    // Order matches that of Iids property above.
    public static readonly (Type Interface, Type Impl, int VTableTotalLength, int VTableFirstInstance)[] Impls = new[]
    {
        (typeof(IComInterface1), typeof(IComInterface1.Impl), 4, 3),
        (typeof(IComInterface2), typeof(IComInterface2.Impl), 5, 3),
        (typeof(IComInterface3), typeof(IComInterface3.Impl), 4, 3),
    };
}

public partial interface IComInterface1 : IUnmanagedInterfaceType<InterfaceId>
{
    static InterfaceId IUnmanagedInterfaceType<InterfaceId>.TypeKey => new(new Guid(ComProxies.Iids.Slice(0 * 16, 16)));
    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface1, IUnmanagedInterfaceType<InterfaceId>
    {
        void IComInterface1.Method()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider<InterfaceId>)this).GetVirtualMethodTableInfoForKey<IComInterface1>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[ComProxies.Impls[0].VTableFirstInstance])(thisPtr);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}

public partial interface IComInterface2 : IUnmanagedInterfaceType<InterfaceId>
{
    static InterfaceId IUnmanagedInterfaceType<InterfaceId>.TypeKey => new(new Guid(ComProxies.Iids.Slice(1 * 16, 16)));

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface2
    {
        void IComInterface2.Method1()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider<InterfaceId>)this).GetVirtualMethodTableInfoForKey<IComInterface2>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[ComProxies.Impls[1].VTableFirstInstance])(thisPtr);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
        void IComInterface2.Method2()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider<InterfaceId>)this).GetVirtualMethodTableInfoForKey<IComInterface2>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[ComProxies.Impls[1].VTableFirstInstance + 1])(thisPtr);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}

public partial interface IComInterface3 : IUnmanagedInterfaceType<InterfaceId>
{
    static InterfaceId IUnmanagedInterfaceType<InterfaceId>.TypeKey => new(new Guid(ComProxies.Iids.Slice(2 * 16, 16)));

    [DynamicInterfaceCastableImplementation]
    internal interface Impl : IComInterface3, IUnmanagedInterfaceType<InterfaceId>
    {
        void IComInterface3.Method()
        {
            unsafe
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider<InterfaceId>)this).GetVirtualMethodTableInfoForKey<IComInterface3>();
                int hr = ((delegate* unmanaged<nint, int>)vtable[ComProxies.Impls[2].VTableFirstInstance])(thisPtr);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
#endregion Generated

public unsafe class Program
{
    static readonly void*[] tables = new void*[3];
    static readonly void*[] impls = new void*[3];

    [UnmanagedCallersOnly]
    static int QueryInterface(void* thisPtr, Guid* iid, void** ppObj)
    {
        if (*iid == GetTypeKey<IComInterface1>())
        {
            *ppObj = impls[0];
        }
        else if (*iid == GetTypeKey<IComInterface2>())
        {
            *ppObj = impls[1];
        }
        else if (*iid == GetTypeKey<IComInterface3>())
        {
            *ppObj = impls[2];
        }
        else
        {
            Debug.Fail("QI failed");
            return -1;
        }

        Console.WriteLine($"--- {nameof(QueryInterface)}");
        Marshal.AddRef((nint)thisPtr);
        return 0;

        static Guid GetTypeKey<T>()
            where T : IUnmanagedInterfaceType<InterfaceId>
        {
            return T.TypeKey.Iid;
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

    private static void Main(string[] args)
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
            void** instance = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), sizeof(void*));

            // Set up the instance with the vtable
            instance[0] = tables[i];
            impls[i] = instance;
        }

        // Test the instances
        for (int i = 0; i < impls.Length; ++i)
        {
            Console.WriteLine($"=== Instance {i}");
            Run(new MyComObject(impls[i]));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Run(object obj)
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
}