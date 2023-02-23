using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

internal sealed unsafe partial class MyGeneratedComWrappers : StrategyBasedComWrappers
{
    private ComInterfaceEntry* s_vtableA = null;
    private ComInterfaceEntry* VTableA
    {
        get
        {
            if (s_vtableA != null)
                return s_vtableA;
            IIUnknownInterfaceDetailsStrategy detailsStrategy = GetOrCreateInterfaceDetailsStrategy();
            ComInterfaceEntry* vtable = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(A), sizeof(ComInterfaceEntry) * 1);
            IIUnknownDerivedDetails? details;
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface1).TypeHandle);
            if (details is not null)
            {
                vtable[0].IID = details.Iid;
                vtable[0].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            return s_vtableA = vtable;
        }
    }

    private ComInterfaceEntry* s_vtableB = null;
    private ComInterfaceEntry* VTableB
    {
        get
        {
            if (s_vtableB != null)
                return s_vtableB;
            IIUnknownInterfaceDetailsStrategy detailsStrategy = GetOrCreateInterfaceDetailsStrategy();
            ComInterfaceEntry* vtable = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(B), sizeof(ComInterfaceEntry) * 2);
            IIUnknownDerivedDetails? details;
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface1).TypeHandle);
            if (details is not null)
            {
                vtable[0].IID = details.Iid;
                vtable[0].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface3).TypeHandle);
            if (details is not null)
            {
                vtable[1].IID = details.Iid;
                vtable[1].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            return s_vtableB = vtable;
        }
    }

    private ComInterfaceEntry* s_vtableC = null;
    private ComInterfaceEntry* VTableC
    {
        get
        {
            if (s_vtableC != null)
                return s_vtableC;
            IIUnknownInterfaceDetailsStrategy detailsStrategy = GetOrCreateInterfaceDetailsStrategy();
            ComInterfaceEntry* vtable = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(C), sizeof(ComInterfaceEntry) * 2);
            IIUnknownDerivedDetails? details;
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface2).TypeHandle);
            if (details is not null)
            {
                vtable[0].IID = details.Iid;
                vtable[0].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(INotAComInterface).TypeHandle);
            if (details is not null)
            {
                vtable[1].IID = details.Iid;
                vtable[1].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            return s_vtableC = vtable;
        }
    }

    private ComInterfaceEntry* s_vtableD = null;
    private ComInterfaceEntry* VTableD
    {
        get
        {
            if (s_vtableD != null)
                return s_vtableD;
            IIUnknownInterfaceDetailsStrategy detailsStrategy = GetOrCreateInterfaceDetailsStrategy();
            ComInterfaceEntry* vtable = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D), sizeof(ComInterfaceEntry) * 2);
            IIUnknownDerivedDetails? details;
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface1).TypeHandle);
            if (details is not null)
            {
                vtable[0].IID = details.Iid;
                vtable[0].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            details = detailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface4).TypeHandle);
            if (details is not null)
            {
                vtable[1].IID = details.Iid;
                vtable[1].Vtable = (nint)details.ManagedVirtualMethodTable;
            }
            return s_vtableD = vtable;
        }
    }

    protected sealed override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        if (obj.GetType() == typeof(A))
        {
            count = 1;
            return VTableA;
        }
        if (obj.GetType() == typeof(B))
        {
            count = 2;
            return VTableB;
        }
        if (obj.GetType() == typeof(C))
        {
            count = 1;
            return VTableC;
        }
        if (obj.GetType() == typeof(D))
        {
            count = 2;
            return VTableD;
        }
        count = 0;
        return null;
    }
}
