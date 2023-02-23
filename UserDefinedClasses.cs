using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

class A : IComInterface1
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

class B : IComInterface1, IComInterface3
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

class C : IComInterface2, INotAComInterface
{
    void IComInterface2.Method1()
    {
        Console.WriteLine("--- B.IComInterface2.Method1");
    }
    void IComInterface2.Method2()
    {
        Console.WriteLine("--- B.IComInterface2.Method2");
    }
    void INotAComInterface.Method()
    {
        Console.WriteLine("--- B.INotAComInterface.Method");
    }
}

class D : IComInterface4
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

[GeneratedComClass(typeof(A))]
[GeneratedComClass(typeof(B))]
[GeneratedComClass(typeof(C))]
[GeneratedComClass(typeof(D))]
internal sealed partial class MyGeneratedComWrappers : StrategyBasedComWrappers
{
}