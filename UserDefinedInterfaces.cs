using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[GeneratedComInterface<MyGeneratedComWrappers>]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface1
{
    void Method();
}

[GeneratedComInterface<MyGeneratedComWrappers>]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface2
{
    void Method1();
    void Method2();
}

[GeneratedComInterface<MyGeneratedComWrappers>]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface3
{
    void Method();
}

internal sealed partial class MyGeneratedComWrappers : GeneratedComWrappersBase
{
}