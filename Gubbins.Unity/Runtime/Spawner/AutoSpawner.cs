using System;
using UnityEngine;

namespace Gubbins.Spawner
{
    [Serializable]
    public class AutoSpawner<T> : ISpawner<T>
    {
        private static readonly ISpawner<T> s_Instance;

        static AutoSpawner()
        {
            if (typeof(T).IsAssignableFrom(typeof(ScriptableObject)))
            {
                s_Instance = CreateGenericInstance(typeof(ScriptableSpawner<>));
            }
            else if (typeof(T).IsAssignableFrom(typeof(Component)))
            {
                s_Instance = CreateGenericInstance(typeof(ComponentSpawner<>));
            }
            else if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                s_Instance = CreateGenericInstance(typeof(NewableSpawner<>));
            }
            else
            {
                s_Instance = new UninitializedSpawner<T>();
            }

            return;

            static ISpawner<T> CreateGenericInstance(Type genericType)
            {
                var instanceType = genericType.MakeGenericType(typeof(T));
                var instance = Activator.CreateInstance(instanceType) as ISpawner<T>;
                return instance ?? throw new InvalidOperationException($"Failed to create instance of {genericType}");
            }
        }

        public T Spawn() => s_Instance.Spawn();

        object ISpawner.Spawn() => Spawn();
    }
}