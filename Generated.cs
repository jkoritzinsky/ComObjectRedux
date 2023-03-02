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

internal sealed partial class MyGeneratedComWrappers : StrategyBasedComWrappers
{
    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        => throw new NotImplementedException();
}

file unsafe static class ComInterfaceInformation
{
    // Nested classes for each namespace and class level
    internal sealed class IComInterface1 : IIUnknownInterfaceType
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
                    Impl.PopulateManagedVirtualMethodTable(m_vtable);
                }
                return m_vtable;
            }
        }

        [DynamicInterfaceCastableImplementation]
        internal interface Impl : global::IComInterface1
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
    }

    public sealed class IComInterface2 : IIUnknownInterfaceType
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
                    Impl.PopulateManagedVirtualMethodTable(m_vtable);
                }
                return m_vtable;
            }
        }

        [DynamicInterfaceCastableImplementation]
        internal interface Impl : global::IComInterface2
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
    }
    public sealed class IComInterface3 : IIUnknownInterfaceType
    {
        public static Guid Iid => new Guid("2c3f9903-b586-46b1-881b-adfce9af47b3");


        private static readonly void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(global::IComInterface3), sizeof(void*) * 4);

        public static void** ManagedVirtualMethodTable
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
        internal interface Impl : global::IComInterface3
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
                    global::IComInterface3 @this = (global::IComInterface3)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
                    @this.Method();
                }
                catch (System.Exception ex)
                {
                    retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
                }
                return retVal;
            }

            void global::IComInterface3.Method()
            {
                unsafe
                {
                    var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface3));
                    int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
            }
        }
    }
    public sealed class IComInterface4 : IIUnknownInterfaceType
    {
        public static Guid Iid => new Guid("793d3e1c-f69d-492b-82fe-984a126e3a8f");

        private static readonly void** m_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(global::IComInterface4), sizeof(void*) * 5);

        public static void** ManagedVirtualMethodTable
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
        internal interface Impl : global::IComInterface4
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
                    global::IComInterface4 @this = (global::IComInterface4)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
                    @this.Method();
                }
                catch (System.Exception ex)
                {
                    retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
                }
                return retVal;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })]
            private static int ABI_DerivedMethod(void* thisPtr)
            {
                int retVal = 0;
                try
                {
                    global::IComInterface4 @this = (global::IComInterface4)ComWrappersUnwrapper.GetObjectForUnmanagedWrapper(thisPtr);
                    @this.DerivedMethod();
                }
                catch (System.Exception ex)
                {
                    retVal = ExceptionHResultMarshaller<int>.ConvertToUnmanaged(ex);
                }
                return retVal;
            }

            void global::IComInterface1.Method()
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface4));
                int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            void global::IComInterface4.DerivedMethod()
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(global::IComInterface4));
                int hr = ((delegate* unmanaged<void*, int>)vtable[3])(thisPtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}

[IUnknownDerived<ComInterfaceInformation.IComInterface1, ComInterfaceInformation.IComInterface1.Impl>]
public unsafe partial interface IComInterface1
{
}

[IUnknownDerived<ComInterfaceInformation.IComInterface2, ComInterfaceInformation.IComInterface2.Impl>]
public unsafe partial interface IComInterface2
{
}

[IUnknownDerived<ComInterfaceInformation.IComInterface3, ComInterfaceInformation.IComInterface3.Impl>]
public unsafe partial interface IComInterface3
{
}

[IUnknownDerived<ComInterfaceInformation.IComInterface4, ComInterfaceInformation.IComInterface4.Impl>]
public unsafe partial interface IComInterface4
{
}