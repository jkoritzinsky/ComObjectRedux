// Types for the COM Source Generator that define the basic vtable interactions and that we would need in the COM source generator in one form or another.
namespace System.Runtime.InteropServices.Marshalling;

/// <summary>
/// Information about a virtual method table and the unmanaged instance pointer.
/// </summary>
public readonly unsafe struct VirtualMethodTableInfo
{
    /// <summary>
    /// Construct a <see cref="VirtualMethodTableInfo"/> from a given instance pointer and table memory.
    /// </summary>
    /// <param name="instance">The pointer to the instance.</param>
    /// <param name="virtualMethodTable">The block of memory that represents the virtual method table.</param>
    public VirtualMethodTableInfo(void* instance, void** virtualMethodTable)
    {
        Instance = instance;
        VirtualMethodTable = virtualMethodTable;
    }

    /// <summary>
    /// The unmanaged instance pointer
    /// </summary>
    public void* Instance { get; }

    /// <summary>
    /// The virtual method table.
    /// </summary>
    public void** VirtualMethodTable { get; }

    /// <summary>
    /// Deconstruct this structure into its two fields.
    /// </summary>
    /// <param name="thisPointer">The <see cref="Instance"/> result</param>
    /// <param name="virtualMethodTable">The <see cref="VirtualMethodTable"/> result</param>
    public void Deconstruct(out void* thisPointer, out void** virtualMethodTable)
    {
        thisPointer = Instance;
        virtualMethodTable = VirtualMethodTable;
    }
}

/// <summary>
/// This interface allows an object to provide information about a virtual method table for a managed interface to enable invoking methods in the virtual method table.
/// </summary>
public unsafe interface IUnmanagedVirtualMethodTableProvider
{
    /// <summary>
    /// Get the information about the virtual method table for a given unmanaged interface type represented by <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The managed type for the unmanaged interface.</param>
    /// <returns>The virtual method table information for the unmanaged interface.</returns>
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);
}
