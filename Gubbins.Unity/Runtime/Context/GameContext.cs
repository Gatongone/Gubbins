using System;
using System.Linq;
using Gubbins.Context;
using Gubbins.Enhance;
using UnityEngine;

[CreateAssetMenu(fileName = "GameContext", menuName = "Context/GameContext")]
public class GameContext : ScriptableObject, IContext, IDependenciesRegistry
{
    private ApplicationContext m_Context;

    public IContext Parent => m_Context.Parent;

    [SerializeField] private SerializedReference<IDependenciesInstaller>[] m_Installers;

    private IDependenciesRegistry m_Registry => m_Context;
    private IDependenciesResolver m_Resolver => m_Context;

    private void Awake() => m_Context = new ApplicationContext(m_Installers.Where(item => item is {Value: not null})
                                                                           .Select(static item => item.Value), ApplicationContext.Global);

    public object Resolve(Type type, string key) => m_Resolver.Resolve(type, key);

    public object[] ResolveAll(Type type) => m_Resolver.ResolveAll(type);

    public void Dispose() => m_Context.Dispose();

    public IBindingDecorator Register(Type type) => m_Registry.Register(type);

    public INotMultitonBindingDecorator Register(object instance) => m_Registry.Register(instance);

    public IMultitonBindingDecorator Register(object[] instances) => m_Registry.Register(instances);
}