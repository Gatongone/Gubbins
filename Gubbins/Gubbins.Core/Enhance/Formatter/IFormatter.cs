namespace Gubbins.Enhance;

/// <summary>
/// A serializer that can convert a source type into a specific material type. The material type is covariant,
/// meaning that it can produce a more specific type than the one specified. This allows for greater flexibility in serialization,
/// as the serializer can handle a wider range of source types while still producing the desired material type.
/// </summary>
/// <typeparam name="TMaterial"></typeparam>
public interface ISerializer<out TMaterial>
{
    /// <summary>
    /// Serializes the given source object into the material type specified by <c>TMaterial</c>. The method takes a source object of any type TTarget
    /// </summary>
    /// <param name="source">The source object to be serialized. It can be of any type TTarget, allowing for flexibility in the types of objects that can be serialized.</param>
    /// <typeparam name="TTarget">The type of the source object to be serialized. This is a generic type parameter that allows for any type to be used as the source for serialization.</typeparam>
    /// <returns>Returns an instance of the material type <c>TMaterial</c> that represents the serialized form of the source object.</returns>
    TMaterial Serialize<TTarget>(TTarget source);
}

/// <summary>
/// A deserializer that can convert a specific material type into a target type. The material type is contravariant,
/// meaning that it can accept a more general type than the one specified. This allows for greater flexibility in deserialization,
/// as the deserializer can handle a wider range of material types while still producing the desired target type.
/// </summary>
/// <typeparam name="TMaterial"> The type of material that this deserializer can handle. It is contravariant, allowing for more general types to be accepted.</typeparam>
public interface IDeserializer<in TMaterial>
{
    /// <summary>
    /// Deserializes the given material into an instance of the target type specified by <c>TTarget</c>. The method takes a material of type <c>TMaterial</c>
    /// </summary>
    /// <param name="material">The material to be deserialized. It is of type <c>TMaterial</c>, which is the type that this deserializer can handle.</param>
    /// <typeparam name="TTarget"> The type of the target object to be deserialized. This is a generic type parameter that allows for any type to be used as the target for deserialization.</typeparam>
    /// <returns>Returns an instance of the target type <c>TTarget</c> that represents the deserialized form of the material.</returns>
    TTarget Deserialize<TTarget>(TMaterial material);
}

/// <summary>
/// A formatter that combines both serialization and deserialization capabilities for a specific material type.
/// </summary>
/// <typeparam name="TMaterial">The type of material that this formatter can handle for both serialization and deserialization.</typeparam>
public interface IFormatter<TMaterial> : ISerializer<TMaterial>, IDeserializer<TMaterial>;