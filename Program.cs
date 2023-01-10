using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

public unsafe class Program
{
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
            for (int i = 0; i < instances.Length; ++i)
            {
                Console.WriteLine($"=== Instance {i}");
                var rcw = new MyComObject(instances[i]); // This would be replaced with a ComWrappers implementation.
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
        }
    }

#region Unmanaged code region
    static readonly void*[] tables = new void*[3];
    static readonly void*[] impls = new void*[3];

    const nint SupportNone = 0;
    const nint SupportComInterface1 = 1;
    const nint SupportComInterface2 = 2;
    const nint SupportComInterface3 = 4;

    [UnmanagedCallersOnly]
    static int QueryInterface(void* thisPtr, Guid* iid, void** ppObj)
    {
        var inst = new ReadOnlySpan<nint>(thisPtr, 2);
        if (*iid == GetTypeKey<IComInterface1>() && (inst[1] & SupportComInterface1) != 0)
        {
            *ppObj = impls[0];
        }
        else if (*iid == GetTypeKey<IComInterface2>() && (inst[1] & SupportComInterface2) != 0)
        {
            *ppObj = impls[1];
        }
        else if (*iid == GetTypeKey<IComInterface3>() && (inst[1] & SupportComInterface3) != 0)
        {
            *ppObj = impls[2];
        }
        else
        {
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            return E_NOINTERFACE;
        }

        Console.WriteLine($"--- {nameof(QueryInterface)}");
        Marshal.AddRef((nint)thisPtr);
        return 0;

        static Guid GetTypeKey<T>()
            where T : IIUnknownInterfaceType
        {
            return T.Iid;
        }
    }

    [UnmanagedCallersOnly]
    static uint AddRef(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(AddRef)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static uint Release(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(Release)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI1_Method(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI1_Method)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI2_Method1(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI2_Method1)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI2_Method2(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI2_Method2)}");
        return 0;
    }

    [UnmanagedCallersOnly]
    static int CI3_Method(void* thisPtr)
    {
        Console.WriteLine($"--- {nameof(CI3_Method)}");
        return 0;
    }

    private static void*[] ActivateNativeCOMInstances()
    {
        void** table;
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 4 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI1_Method;
            tables[0] = table;
        }
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 5 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI2_Method1;
            table[4] = (delegate* unmanaged<void*, int>)&CI2_Method2;
            tables[1] = table;
        }
        {
            table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 4 * sizeof(void*));
            table[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            table[1] = (delegate* unmanaged<void*, uint>)&AddRef;
            table[2] = (delegate* unmanaged<void*, uint>)&Release;
            table[3] = (delegate* unmanaged<void*, int>)&CI3_Method;
            tables[2] = table;
        }

        // Build the instances
        for (int i = 0; i < impls.Length; ++i)
        {
            void** instance = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Program), 2 * sizeof(void*));

            // Define which interfaces for each instance
            var inst = new Span<nint>(instance, 2);
            inst[1] = i switch
            {
                0 => SupportComInterface1,
                1 => SupportComInterface1 | SupportComInterface3,
                2 => SupportComInterface2,
                _ => SupportNone
            };

            // Set up the instance with the vtable
            instance[0] = tables[i];
            impls[i] = instance;
        }

        return impls;
    }
#endregion Unmanaged code region
}
