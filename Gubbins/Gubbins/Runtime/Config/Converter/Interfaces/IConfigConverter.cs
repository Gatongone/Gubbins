using System;

namespace Gubbins.Config;

public interface IConfigConverter
{
    object Serialize(object source);
    object Desierlize(Type targetType, object source);
}