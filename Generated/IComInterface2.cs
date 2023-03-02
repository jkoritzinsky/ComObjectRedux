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
    public static Guid Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b2");

    private static readonly void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(global::IComInterface2), sizeof(void*) * 5);

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
file unsafe interface InterfaceImplementation : global::IComInterface2
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
            global::IComInterface2 @this = (global::IComInterface2)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
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
            global::IComInterface2 @this = (global::IComInterface2)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
            @this.Method2();
        }
        catch (System.Exception ex)
        {
            retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
        }
        return retVal;
    }

    void global::IComInterface2.Method1()
    {
        unsafe
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface2));
            int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
    void global::IComInterface2.Method2()
    {
        unsafe
        {
            var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface2));
            int hr = ((delegate* unmanaged<void*, int>)vtable[4])(thisPtr);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}

[IUnknownDerived<InterfaceInformation, InterfaceImplementation>]
public unsafe partial interface IComInterface2
{
}