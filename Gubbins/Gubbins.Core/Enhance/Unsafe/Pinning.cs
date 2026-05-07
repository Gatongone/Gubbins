namespace Gubbins.Enhance;

/// <summary>
/// Provides pinning object reference.
/// </summary>
public readonly unsafe struct Pinning
{
    /// <summary>
    /// The object reference.
    /// </summary>
    private readonly void* m_SourceRef;

    /// <param name="source">Object type reference.</param>
    public Pinning(void* source) => m_SourceRef = source;

    /// <param name="source">Object type reference.</param>
    public Pinning(object source) => m_SourceRef = Native.GetAddress(source);

    /// <summary>
    /// Returns a reference to an object of type T that can be used for pinning.
    /// </summary>
    /// <remarks>
    /// This method is intended to support .NET compilers and is not intended to be called by user code.
    /// </remarks>
    /// <returns>A reference to an object of type T that can be used for pinning.</returns>
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public ref object GetPinnableReference() => ref *(object*) m_SourceRef;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}