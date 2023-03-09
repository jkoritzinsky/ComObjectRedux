using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

file unsafe class ClassInformation : IComExposedClass
{
    private static ComWrappers.ComInterfaceEntry* _entries = null;

    public static int InterfaceEntriesLength => 2;
    static unsafe ComWrappers.ComInterfaceEntry* IComExposedClass.ComInterfaceEntries
    {
        get
        {
            if (_entries == null)
            {
                ComWrappers.ComInterfaceEntry* vtable = (ComWrappers.ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(B), sizeof(ComWrappers.ComInterfaceEntry) * 2);
                IIUnknownDerivedDetails? details;
                details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface1).TypeHandle)!;
                vtable[0].IID = details.Iid;
                vtable[0].Vtable = (nint)details.ManagedVirtualMethodTable;
                details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface3).TypeHandle)!;
                vtable[1].IID = details.Iid;
                vtable[1].Vtable = (nint)details.ManagedVirtualMethodTable;
                _entries = vtable;
            }
            return _entries;
        }
    }
}

[ComExposedClass<ClassInformation>]
partial class B
{
}
