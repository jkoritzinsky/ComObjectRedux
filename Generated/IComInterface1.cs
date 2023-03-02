using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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

file sealed unsafe class InterfaceInformation : IIUnknownInterfaceType
{
    public static Guid Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b1");

    private static readonly void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(global::IComInterface1), sizeof(void*) * 4);

    public static void** ManagedVirtualMethodTable
    {
        get
        {
            if (m_vtable[0] == null)
            {
                IUnknownVTableComWrappers.GetIUnknownImpl(out m_vtable[0], out m_vtable[1], out m_vtable[2]);
                InterfaceImplementation.PopulateManagedVirtualMethodTable(m_vtable);
            }
            return m_vtable;
        }
    }
}

[DynamicInterfaceCastableImplementation]
file unsafe interface InterfaceImplementation : global::IComInterface1
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
            global::IComInterface1 @this = (global::IComInterface1)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
            @this.Method();
        }
        catch (System.Exception ex)
        {
            retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
        }
        return retVal;
    }

    void global::IComInterface1.Method()
    {
        var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface1));
        int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}

[IUnknownDerived<InterfaceInformation, InterfaceImplementation>]
public unsafe partial interface IComInterface1
{
}