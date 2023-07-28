using System;
namespace Gubbins.Config;

public class ConfigConvertAttribute : Attribute
{
    private readonly Type ConverterType;

    public ConfigConvertAttribute(Type converterType)
    {
        if (!typeof(IConfigConverter).IsAssignableFrom(converterType))
            throw new ArgumentException("The type is not a IConfigConverter type");
        ConverterType = converterType;
    }
}

public class ConfigConvertAttribute<T> : Attribute where T : IConfigConverter
{
    private readonly Type ConverterType = typeof(T);
}