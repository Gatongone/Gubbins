using System.Xml;
using Gubbins.Enhance;
using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// Xml installer implementation.
/// </summary>
/// <remarks>
/// Xml installer is used to deserialize and store configuration data from an XML file. The XML format is as follows:
/// <code>
/// &lt;?xml version="1.0" encoding="UTF-8" ?&gt;
/// &lt;Configs &gt;
///    &lt;Config
///         key="{Key}"
///         type="{Type_FullName}"
///         scope="{ScopeType}"
///         spawner="{SpawnerType_FullName}"
///         bake="{Bake_Count (Only Multiton)}"
///         controller="{ControllerType_FullName (Only Custom)}"&gt;
///        &lt;Binding type="{BindingType1_FullName}"/&gt;
///        &lt;Binding type="{BindingType2_FullName}"/&gt;
///        &lt;Binding type="{BindingType3_FullName}"/&gt;
///    &lt;/Config&gt;
/// 
/// &lt;!-- ... more configs... --&gt;
/// &lt;/Configs&gt;
/// </code>
/// </remarks>
public sealed class XmlContext(XmlDocument doc) : IDependenciesInstaller
{
    public void Install(IDependenciesRegistry registry)
    {
        var installInfos = Deserialize<IEnumerable<InstallInfo>>();

        foreach (var info in installInfos)
        {
            registry.Register(info);
        }
    }

    public TTarget Deserialize<TTarget>()
    {
        // Sanity check to ensure the XML is valid and can be deserialized into InstallInfo.
        if (typeof(TTarget) != typeof(IEnumerable<InstallInfo>)) throw new InvalidOperationException("Can only deserialize to IEnumerable<InstallInfo>");

        var results = new List<InstallInfo>();

        foreach (var element in doc["Configs"].SelectNodes("Config")!.Cast<XmlElement>())
        {
            results.Add(BuildInstallInfo(element));
        }

        return (TTarget) (object) results;

        static InstallInfo BuildInstallInfo(XmlElement element)
        {
            var result = new InstallInfo();

            var scope = element.Attributes["scope"]?.Value;
            var key = element.Attributes["Key"]?.Value;
            var type = element.Attributes["type"]?.Value;
            var spawner = element.Attributes["spawner"]?.Value;
            var controller = element.Attributes["controller"]?.Value;
            var bake = element.Attributes["bake"]?.Value;
            var bindings = element.Attributes["Bindings"]?.SelectNodes("Binding")?.Cast<XmlElement>();

            if (type == null) throw new ArgumentException("Missing or invalid 'Type' attribute in XML");

            result.Key        = key ?? type ?? throw new ArgumentException("Missing or invalid 'Key' attribute in XML");
            result.Type       = Type.GetType(type, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!;
            result.Scope      = string.IsNullOrEmpty(scope) ? Scope.Singleton : Enum.Parse<Scope>(scope);
            result.Spawner    = spawner == null ? result.Type.IsNewable(out var newable) ? newable.ToSpawner() : throw new ArgumentException($"Non spawner for type: \"{result.Type}\"") : (ISpawner) Activator.CreateInstance(Type.GetType(spawner, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!);
            result.Controller = controller == null ? null : Activator.CreateInstance(Type.GetType(controller, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!) as IScopeController;
            result.BakeCount  = bake == null ? 0 : uint.Parse(bake);
            result.Bindings   = bindings == null ? [] : bindings.Select(binding => Type.GetType(binding.Attributes["type"]!.Value, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!).ToHashSet();
            return result;
        }
    }
}