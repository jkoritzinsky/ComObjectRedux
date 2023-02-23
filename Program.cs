using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

public unsafe class Program
{
    private static readonly ComWrappers s_comWrappers = new MyGeneratedComWrappers();

    private static void Main(string[] args)
    {
        // Activate native COM instances
        void*[] instances = ActivateNativeCOMInstances();

        // Test the instances
        Run(instances);

        // Clean up the RCWs
        GC.Collect();
        GC.WaitForPendingFinalizers();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Run(void*[] instances)
        {
            var comWrappers = new MyGeneratedComWrappers();
            for (int i = 0; i < instances.Length; ++i)
            {
                Console.WriteLine($"=== Instance {i}");
                var rcw = s_comWrappers.GetOrCreateObjectForComInstance((nint)instances[i], CreateObjectFlags.None);
                Debug.Assert(rcw is ComObject); // Ensure that we didn't unwrap the CCW and instead created an RCW around it.
                InspectObject(rcw);
                InspectObject(rcw);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void InspectObject(object obj)
        {
            if (obj is IComInterface1 c1)
            {
                c1.Method();
            }
            if (obj is IComInterface2 c2)
            {
                c2.Method1();
                c2.Method2();
            }
            if (obj is IComInterface3 c3)
            {
                c3.Method();
            }
            if (obj is IComInterface4 c4)
            {
                c4.Method();
                c4.DerivedMethod();
            }
        }
    }

    private static void*[] ActivateNativeCOMInstances()
    {
        // Build the instances
        void*[] instances = new void*[4];
        instances[0] = (void*)s_comWrappers.GetOrCreateComInterfaceForObject(new A(), CreateComInterfaceFlags.None);
        instances[1] = (void*)s_comWrappers.GetOrCreateComInterfaceForObject(new B(), CreateComInterfaceFlags.None);
        instances[2] = (void*)s_comWrappers.GetOrCreateComInterfaceForObject(new C(), CreateComInterfaceFlags.None);
        instances[3] = (void*)s_comWrappers.GetOrCreateComInterfaceForObject(new D(), CreateComInterfaceFlags.None);
        return instances;
    }
}
