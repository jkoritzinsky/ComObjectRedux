// Types that are only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling;

/// <summary>
/// A factory to create an unmanaged "this pointer" from a managed object and to get a managed object from an unmanaged "this pointer".
/// </summary>
/// <remarks>
/// This interface would be used by the VTable source generator to enable users to indicate how to get the managed object from the "this pointer".
/// We can hard-code the ComWrappers logic here if we don't want to ship this interface.
/// </remarks>
public unsafe interface IUnmanagedObjectUnwrapper
{
    /// <summary>
    /// Get the object wrapped by <paramref name="ptr"/>.
    /// </summary>
    /// <param name="ptr">A an unmanaged "this pointer".</param>
    /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
    public static abstract object GetObjectForUnmanagedWrapper(void* ptr);
}

/// <summary>
/// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The unmanaged type to convert the HResult to.</typeparam>
/// <remarks>
/// This type is used by the COM source generator to enable marshalling exceptions to the HResult of the exception.
/// We can skip the exposing the exception marshallers if we decide to not expose the VTable source generator.
/// In that case, we'd hard-code the implementations of these marshallers into the COM source generator.
/// </remarks>
#pragma warning disable SYSLIB1055 // The managed type 'System.Exception' for entry-point marshaller type 'System.Runtime.InteropServices.Marshalling.ExceptionHResultMarshaller<T>' must be a closed generic type, have the same arity as the managed type if it is a value marshaller, or have one additional generic parameter if it is a collection marshaller.

[CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionHResultMarshaller<>))]
#pragma warning restore SYSLIB1055
public static class ExceptionHResultMarshaller<T>
    where T : unmanaged, INumber<T>
{
    /// <summary>
    /// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="e">The exception.</param>
    /// <returns>The HResult of the exception, converted to <typeparamref name="T"/>.</returns>
    public static T ConvertToUnmanaged(Exception e)
    {
        // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
        // and calls SetErrorInfo if the platform supports it.

        // We use CreateTruncating here to convert from the int return type of Marshal.GetHRForException
        // to whatever the T is. A "truncating" conversion in this case is the same as an unchecked conversion like
        // (uint)Marshal.GetHRForException(e) would be if we were writing a non-generic marshaller.
        // Since we're using the INumber<T> interface, this is the correct mechanism to represent that conversion.
        return T.CreateTruncating(Marshal.GetHRForException(e));
    }
}

// This type is purely conceptual for the purposes of the Lowered.Example.cs code. We will likely never ship this.
// The analyzer currently doesn't support marshallers where the managed type is 'void'.
#pragma warning disable SYSLIB1057 // The type 'System.Runtime.InteropServices.Marshalling.PreserveSigMarshaller' specifies it supports the 'ManagedToUnmanagedOut' marshal mode, but it does not provide a 'ConvertToManaged' method that takes the unmanaged type as a parameter and returns 'void'.
[CustomMarshaller(typeof(void), MarshalMode.ManagedToUnmanagedOut, typeof(PreserveSigMarshaller))]
#pragma warning restore SYSLIB1057
public static class PreserveSigMarshaller
{
    public static void ConvertToManaged(int hr)
    {
        Marshal.ThrowExceptionForHR(hr);
    }
}

// This is the trigger attribute for the VTable source generator.
// If we decide we want to only expose the COM source generator, then we would keep this attribute internal.
// The current plan is to use this attribute to provide the "don't use the defaults, use this custom logic" options
// for the COM source generator, so if we decide to not expose this, we should provide a different mechanism.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class VirtualMethodIndexAttribute : Attribute
{
    public VirtualMethodIndexAttribute(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public bool ImplicitThisParameter { get; set; } = true;

    /// <summary>
    /// Gets or sets how to marshal string arguments to the method.
    /// </summary>
    /// <remarks>
    /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
    /// <see cref="StringMarshallingCustomType" /> must not be specified.
    /// </remarks>
    public StringMarshalling StringMarshalling { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> used to control how string arguments to the method are marshalled.
    /// </summary>
    /// <remarks>
    /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
    /// or must be set to <see cref="StringMarshalling.Custom" />.
    /// </remarks>
    public Type? StringMarshallingCustomType { get; set; }

    /// <summary>
    /// Gets or sets whether the callee sets an error (SetLastError on Windows or errno
    /// on other platforms) before returning from the attributed method.
    /// </summary>
    public bool SetLastError { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> used to control how an exception is marshalled to the return value.
    /// </summary>
    /// <remarks>
    /// If this field is specified, <see cref="ExceptionMarshalling" /> must not be specified
    /// or must be set to <see cref="ExceptionMarshalling.Custom" />.
    /// </remarks>
    public Type? ExceptionMarshallingType { get; set; }
}

// This attribute provides the mechanism for the VTable source generator to know which type to use to get the managed object
// from the unmanaged "this" pointer. If we decide to not expose VirtualMethodIndexAttribute, we don't need to expose this.
[AttributeUsage(AttributeTargets.Interface)]
public class UnmanagedObjectUnwrapperAttribute<TMapper> : Attribute
    where TMapper : IUnmanagedObjectUnwrapper
{
}

// This type implements the logic to get the managed object from the unmanaged "this" pointer.
// If we decide to not expose the VTable source generator, we don't need to expose this and we can just inline the logic
// into the generated code in the source generator.
public sealed unsafe class ComWrappersUnwrapper : IUnmanagedObjectUnwrapper
{
    public static object GetObjectForUnmanagedWrapper(void* ptr)
    {
        return ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)ptr);
    }
}