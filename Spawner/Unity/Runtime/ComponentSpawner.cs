using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gubbins.Spawner
{
    /// <summary>
    /// A generic spawner for Unity components. This class allows you to spawn instances of a specified component type (T) either
    /// by creating a new GameObject and adding the component to it or by instantiating a prefab that contains the component.
    /// It also provides an option to prevent the spawned GameObject from being destroyed when loading a new scene.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class ComponentSpawner<T> : ISpawner<T> where T : Component
    {
        /// <summary>
        /// Indicates whether the spawned GameObject should not be destroyed when loading a new scene.
        /// </summary>
        public bool DontDestroyOnLoad;

        /// <summary>
        /// The prefab to instantiate when spawning the component. If null, a new GameObject will be created instead.
        /// </summary>
        public GameObject Prefab;

        public ComponentSpawner() { }

        /// <param name="dontDestroyOnLoad">Indicates whether the spawned GameObject should not be destroyed when loading a new scene.</param>
        public ComponentSpawner(bool dontDestroyOnLoad) => DontDestroyOnLoad = dontDestroyOnLoad;

        /// <param name="prefab">The prefab to instantiate when spawning the component. If null, a new GameObject will be created instead.</param>
        /// <param name="dontDestroyOnLoad">Indicates whether the spawned GameObject should not be destroyed when loading a new scene.</param>
        public ComponentSpawner(GameObject prefab, bool dontDestroyOnLoad) => (Prefab, DontDestroyOnLoad) = (prefab, dontDestroyOnLoad);

        /// <summary>
        /// Spawns an instance of the specified component type (T). If a prefab is provided, it will instantiate the prefab; otherwise,
        /// it will create a new GameObject and add the component to it. If DontDestroyOnLoad is set to true, the spawned GameObject will
        /// not be destroyed when loading a new scene.
        /// </summary>
        public T Spawn()
        {
            GameObject go;
            T result;
            if (!Prefab)
            {
                go = new GameObject();
                result = go.AddComponent<T>();
            }
            else
            {
                go = Object.Instantiate(Prefab);
                result = go.GetComponent<T>();
                if (DontDestroyOnLoad)
                {
                    Object.DontDestroyOnLoad(go);
                }
            }

            if (DontDestroyOnLoad)
            {
                Object.DontDestroyOnLoad(go);
            }

            return result;
        }

        /// <summary>
        /// Explicit interface implementation of ISpawner.Spawn() that returns the spawned component as an object.
        /// </summary>
        object ISpawner.Spawn()
        {
            return Spawn();
        }
    }
}