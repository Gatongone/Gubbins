using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// Json context implementation.
/// </summary>
/// <remarks>
/// Json context is used to deserialize and store configuration data from a Json file. The Json format is as follows:
/// <code>
/// [
///     {
///         "key": "{Key_Name}",
///         "type": "{Type_FullName}",
///         "scope": "{ScopeType}",
///         "bake": "{Bake_Count (Only Multiton)}",
///         "controller": "{ControllerType_FullName (Only Custom)}",
///         "bindings":
///         [
///             "{BindingType1_FullName}",
///             "{BindingType2_FullName}",
///             "{BindingType3_FullName}"
///         ]
///     },
///    //... more configs...
/// ]
/// </code>
/// </remarks>
public sealed class JsonContext : ConfigContext<string>
{
    /// <summary>
    /// Json deserializer.
    /// </summary>
    private static readonly IDeserializer<string> s_Formatter = new JsonFormatter();

    /// <param name="config">Json string.</param>
    /// <param name="parent">Parent context.</param>
    public JsonContext(string config, IContext? parent = null) : base(config, s_Formatter, parent) { }

    /// <param name="configs">Json strings.</param>
    /// <param name="parent">Parent context.</param>
    public JsonContext(IEnumerable<string> configs, IContext? parent = null) : base(configs, s_Formatter, parent) { }

    /// <summary>
    /// Json deserializer.
    /// </summary>
    private class JsonFormatter : IDeserializer<string>
    {
        /// <inheritdoc />
        public TTarget Deserialize<TTarget>(string json)
        {
            // Sanity check to ensure the Json is valid and can be deserialized into InstallInfo.
            if (typeof(TTarget) != typeof(IEnumerable<InstallInfo>))
            {
                throw new InvalidOperationException("Can only deserialize to IEnumerable<InstallInfo>");
            }

            var results = new List<InstallInfo>();

            json = json.Trim().Trim('[', ']').Trim();
            foreach (var element in SplitJsonObjects(json))
            {
                if (string.IsNullOrWhiteSpace(element))
                {
                    continue;
                }

                results.Add(ParseSingleObject(element));
            }

            return (TTarget) (object) results;
        }
    }

    /// <summary>
    /// Split Json objects into individual strings.
    /// </summary>
    private static List<string> SplitJsonObjects(string json)
    {
        var objects = new List<string>();
        var braceCount = 0;
        var startIndex = -1;

        for (var i = 0; i < json.Length; i++)
        {
            if (json[i] == '{')
            {
                if (braceCount == 0) startIndex = i;
                braceCount++;
            }
            else if (json[i] == '}')
            {
                braceCount--;
                if (braceCount == 0 && startIndex != -1)
                {
                    objects.Add(json.Substring(startIndex, i - startIndex + 1));
                    startIndex = -1;
                }
            }
        }

        return objects;
    }

    /// <summary>
    /// Parse Json object.
    /// </summary>
    private static InstallInfo ParseSingleObject(string json)
    {
        var config = new InstallInfo();

        // Remove leading and trailing curly braces and spaces from the Json string.
        json = json.Trim().Trim('{', '}').Trim();

        // Split property
        var properties = Pool<List<string>>.Default.Spawn();
        SplitProperties(json, properties);
        foreach (var prop in properties)
        {
            if (string.IsNullOrWhiteSpace(prop)) continue;

            var keyValue = SplitKeyValue(prop);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().Trim('"');
            var value = keyValue[1].Trim();

            switch (key.ToLower())
            {
                case "key":
                    config.Key = value.Trim('"');
                    break;
                case "type":
                    var type = value.Trim('"');
                    config.Type = Type.GetType(type, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver) ?? throw new ArgumentException($"Invalid type: {type}");
                    break;
                case "scope":
                    config.Scope = Enum.Parse<Scope>(value.Trim('"'));
                    break;
                case "bake":
                    if (uint.TryParse(value, out var bakeValue))
                        config.BakeCount = bakeValue;
                    else throw new ArgumentException($"Invalid Bake Count: {value}");
                    break;
                case "controller":
                    var controllerType = value.Trim('"');
                    config.Controller = Activator.CreateInstance(Type.GetType(controllerType, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!) as IScopeController ?? throw new ArgumentException($"Invalid Controller type: {controllerType}");
                    break;
                case "spawner":
                    var spawnerType = Type.GetType(value.Trim('"'));
                    if (spawnerType != null)
                    {
                        config.Spawner = Activator.CreateInstance(spawnerType) as ISpawner;
                    }

                    break;
                case "bindings":
                    config.Bindings = ParseStringArray(value).Select(binding => Type.GetType(binding.Trim('"'), Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)).ToHashSet();
                    break;
            }
        }

        Pool<List<string>>.Default.Recycle(properties);

        if (config.Spawner == null)
        {
            if (!config.Type!.IsNewable(out var newable))
            {
                throw new ArgumentException($"Non spawner for type: \"{config.Type}\"");
            }

            config.Spawner = newable.ToSpawner();
        }

        config.Key ??= config.Type!.FullName;
        return config;
    }

    /// <summary>
    /// Split properties into individual strings.
    /// </summary>
    private static void SplitProperties(string json, List<string> properties)
    {
        var braceCount = 0;
        var bracketCount = 0;
        var startIndex = 0;
        var inQuotes = false;

        for (var i = 0; i < json.Length; i++)
        {
            if (json[i] == '"' && (i == 0 || json[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }

            if (inQuotes) continue;
            switch (json[i])
            {
                case '{':
                    braceCount++;
                    break;
                case '}':
                    braceCount--;
                    break;
                case '[':
                    bracketCount++;
                    break;
                case ']':
                    bracketCount--;
                    break;
                case ',' when braceCount == 0 && bracketCount == 0:
                    properties.Add(json.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                    break;
            }
        }

        if (startIndex < json.Length)
        {
            properties.Add(json.Substring(startIndex));
        }
    }

    /// <summary>
    /// Split key-value pair from Json property.
    /// </summary>
    private static string[] SplitKeyValue(string property)
    {
        var colonIndex = property.IndexOf(':');
        if (colonIndex == -1) return [];

        return
        [
            property[..colonIndex],
            property[(colonIndex + 1)..]
        ];
    }

    /// <summary>
    /// Parse Json array from Json property.
    /// </summary>
    private static List<string> ParseStringArray(string arrayJson)
    {
        var result = new List<string>();

        // Remove leading and trailing square brackets from the Json string.
        arrayJson = arrayJson.Trim().Trim('[', ']').Trim();

        if (string.IsNullOrWhiteSpace(arrayJson)) return result;

        // Split elements by comma.
        var elements = arrayJson.Split(',');
        foreach (var element in elements)
        {
            var trimmed = element.Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}