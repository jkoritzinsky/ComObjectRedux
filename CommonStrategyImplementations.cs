// Implementations of the COM strategy interfaces defined in Com.cs that we would want to ship (can be internal only if we don't want to allow users to provide their own implementations in v1).
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling;


internal sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
{
    public static readonly IIUnknownInterfaceDetailsStrategy Instance = new DefaultIUnknownInterfaceDetailsStrategy();

    public IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
    {
        return IIUnknownDerivedDetails.GetFromAttribute(type);
    }
}

internal sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
{
    public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

    void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
    {
        return unknown;
    }

    unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, Guid handle, out void* ppObj)
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

    unsafe uint IIUnknownStrategy.Release(void* thisPtr)
        => (uint)Marshal.Release((nint)thisPtr);
}

internal sealed unsafe class DefaultCaching : IIUnknownCacheStrategy
{
    // [TODO] Implement some smart/thread-safe caching
    private readonly Dictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new();

    IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails details, void* ptr)
    {
        var obj = (void***)ptr;
        return new IIUnknownCacheStrategy.TableInfo()
        {
            Instance = obj,
            VirtualMethodTable = *obj,
            Implementation = details.Implementation.TypeHandle
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
            _ = unknownStrategy.Release(info.Instance);
        }
        _cache.Clear();
    }
}