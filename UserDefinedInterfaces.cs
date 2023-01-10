using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface1
{
    void Method();
}

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface2
{
    void Method1();
    void Method2();
}

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComInterface3
{
    void Method();
}

// User-defined implementation of ComObject that provides the requested strategy implementations.
// This type will be provided to the source generator through the GeneratedComInterface attribute.
public unsafe class MyComObject : ComObject
{
    internal MyComObject(void* thisPtr)
        : base(DefaultIUnknownInterfaceDetailsStrategy.Instance, FreeThreadedStrategy.Instance, new DefaultCaching())
    {
        // Implementers can, at this point, capture the current thread
        // context and create a proxy for apartment marshalling. The options
        // are to use RoGetAgileReference() on Win 8.1+ or the Global Interface Table (GIT)
        // on pre-Win 8.1.
        //
        // Relevant APIs:
        //  - RoGetAgileReference() - modern way to create apartment proxies
        //  - IGlobalInterfaceTable - GIT interface that helps with proxy management
        //  - CoGetContextToken()   - Low level mechanism for tracking object's apartment context
        //
        // Once the decision has been made to create a proxy (i.e., not free threaded) the
        // implementer should set the instance pointer.
        ThisPtr = thisPtr;
    }
}