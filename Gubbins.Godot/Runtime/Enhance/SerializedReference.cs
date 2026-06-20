using System;
using Godot;

namespace Gubbins.Enhance;

public partial class SerializedReference<T> : Resource where T : class
{
    internal T pureReference;

    internal Node godotReference;

    internal string expectedTypeName;

    public Type ExpectedType => Type.GetType(expectedTypeName);

    public T Value { get; set; }

    public static implicit operator T(SerializedReference<T> reference) => reference.Value;
}