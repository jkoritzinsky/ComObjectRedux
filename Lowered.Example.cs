using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Lowered;

// This file shows how the concept of the generated COM interfaces would be represented using VirtualMethodIndexAttribute directly.
// It's excluded from the compilation as it depends on the generator providing the implementation and I didn't want to have two implementations of the same generated code.

[IUnknownDerived<IComInterface1, Impl>]
[ObjectUnmanagedMapper<ComWrappersWrapperFactory<MyGeneratedComWrappers>>] // Not emitted
public partial interface IComInterface1 : IUnmanagedInterfaceType, IIUnknownInterfaceType
{
    [VirtualMethodIndex(3)] // Not emitted
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })] // Not emitted
    [return: MarshalUsing(typeof(PreserveSigMarshaller))] // Not emitted
    void Method();

    internal partial interface Impl : IComInterface1 {}
}

[IUnknownDerived<IComInterface2, Impl>]
[ObjectUnmanagedMapper<ComWrappersWrapperFactory<MyGeneratedComWrappers>>] // Not emitted
public partial interface IComInterface2 : IUnmanagedInterfaceType, IIUnknownInterfaceType
{
    [VirtualMethodIndex(3)] // Not emitted
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })] // Not emitted
    [return: MarshalUsing(typeof(PreserveSigMarshaller))] // Not emitted
    void Method1();

    [VirtualMethodIndex(4)] // Not emitted
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })] // Not emitted
    [return: MarshalUsing(typeof(PreserveSigMarshaller))] // Not emitted
    void Method2();

    internal partial interface Impl : IComInterface2 {}
}

[IUnknownDerived<IComInterface3, Impl>]
[ObjectUnmanagedMapper<ComWrappersWrapperFactory<MyGeneratedComWrappers>>] // Not emitted
public partial interface IComInterface3 : IUnmanagedInterfaceType, IIUnknownInterfaceType
{
    [VirtualMethodIndex(3)] // Not emitted
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvMemberFunction) })] // Not emitted
    [return: MarshalUsing(typeof(PreserveSigMarshaller))] // Not emitted
    void Method();

    internal partial interface Impl : IComInterface3 {}
}
