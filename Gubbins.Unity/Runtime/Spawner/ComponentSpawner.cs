using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gubbins.Spawner
{
    [Serializable]
    public class ComponentSpawner<T> : ISpawner<T> where T : Component
    {
        public bool       DontDestroyOnLoad;
        public GameObject Prefab;
        public ComponentSpawner() { }

        public ComponentSpawner(bool dontDestroyOnLoad) => DontDestroyOnLoad = dontDestroyOnLoad;

        public ComponentSpawner(GameObject prefab, bool dontDestroyOnLoad) => (Prefab, DontDestroyOnLoad) = (prefab, dontDestroyOnLoad);

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
                go     = Object.Instantiate(Prefab);
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

        object ISpawner.Spawn()
        {
            return Spawn();
        }
    }
}