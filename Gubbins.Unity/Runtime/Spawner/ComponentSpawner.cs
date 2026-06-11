using UnityEngine;

namespace Gubbins.Spawner
{
    public class ComponentSpawner<T> : ISpawner<T> where T : Component
    {
        public bool       DontDestroyOnLoad;
        public GameObject Prefab;
        public ComponentSpawner() { }

        public ComponentSpawner(bool dontDestroyOnLoad) => DontDestroyOnLoad = dontDestroyOnLoad;

        public ComponentSpawner(GameObject prefab, bool dontDestroyOnLoad) => (Prefab, DontDestroyOnLoad) = (prefab, dontDestroyOnLoad);

        public T Spawn()
        {
            var go = default(GameObject);
            var result = default(T);
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