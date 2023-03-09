using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

file unsafe class ClassInformation : IComExposedClass
{
    private static ComWrappers.ComInterfaceEntry* _entries = null;
    public static int InterfaceEntriesLength => 1;
    static unsafe ComWrappers.ComInterfaceEntry* IComExposedClass.ComInterfaceEntries
    {
        get
        {
            if (_entries == null)
            {
                var entries = (ComWrappers.ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(C), sizeof(ComWrappers.ComInterfaceEntry) * 1);
                var details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface2).TypeHandle)!;
                entries[0].IID = details.Iid;
                entries[0].Vtable = (nint)details.ManagedVirtualMethodTable;
                _entries = entries;
            }
            return _entries;
        }
    }
}

[ComExposedClass<ClassInformation>]
partial class C
{
}
