using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[GeneratedComClass]
partial class A : IComInterface1
{
    void IComInterface1.Method()
    {
        Console.WriteLine("--- A.IComInterface1.Method");
    }
}

// If IIUnknownInterfaceType is not generic, then we get an
// "ambiguous best interface implementation" error for all members of
// IIUnknownInterfaceType (since IComInterface1 and IComInterface3 both implement it).
// By making it generic, we can disambiguate the implementations
// while still being able to retrieve the information we need through IUnknownDetailsAttribute.
[GeneratedComClass]
partial class B : IComInterface1, IComInterface3
{
    void IComInterface1.Method()
    {
        Console.WriteLine("--- B.IComInterface1.Method");
    }
    void IComInterface3.Method()
    {
        Console.WriteLine("--- B.IComInterface3.Method");
    }
}

interface INotAComInterface
{
    void Method();
}

[GeneratedComClass]
partial class C : IComInterface2, INotAComInterface
{
    void IComInterface2.Method1()
    {
        Console.WriteLine("--- C.IComInterface2.Method1");
    }
    void IComInterface2.Method2()
    {
        Console.WriteLine("--- C.IComInterface2.Method2");
    }
    void INotAComInterface.Method()
    {
        Console.WriteLine("--- C.INotAComInterface.Method");
    }
}

[GeneratedComClass]
partial class D : IComInterface4
{
    void IComInterface1.Method()
    {
        Console.WriteLine("--- D.IComInterface1.Method");
    }
    void IComInterface4.DerivedMethod()
    {
        Console.WriteLine("--- D.IComInterface4.DerivedMethod");
    }
}