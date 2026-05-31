using System;
using Gubbins.Context;
using UnityEngine;

[CreateAssetMenu(fileName = "GameContext", menuName = "Context/GameContext")]
public class GameContext : ScriptableObject, IContext
{
    public object Resolve(Type type, string key)
    {
        Debug.Log("Resolve called with type: " + type.Name);
        return null;
    }

    public object[] ResolveAll(Type type)
    {
        Debug.Log("ResolveAll called with type: " + type.Name);
        return null;
    }

    public IContext Parent => ApplicationContext.Global;
    public void Dispose() { }
}