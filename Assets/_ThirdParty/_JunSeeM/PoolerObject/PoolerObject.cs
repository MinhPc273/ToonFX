using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace JunEngine
{

    [System.Serializable]
    public class PoolerObject
    {
        [SerializeField] private GameObject ObjectSpawn;
        [SerializeField] private Transform PlaceSpawn;
        [SerializeField] private int PoolSize;

/*        public void SetObjectSpawn(GameObject objectSpawn)
        {
            ObjectSpawn = objectSpawn;
        }*/

        public void CreatePool()
        {
            ObjectPooling.CreatePool(ObjectSpawn, PlaceSpawn, PoolSize);
        }

        public GameObject GetObject()
        {
            GameObject go = ObjectPooling.GetObject(ObjectSpawn, PlaceSpawn);
            return go;
        }

        public T GetObject<T>() where T : Component
        {
            T go = ObjectPooling.GetObject<T>(ObjectSpawn, PlaceSpawn);
            return go;
        }

        public void ReturnObject(GameObject go)
        {
            ObjectPooling.ReturnObject(go);
        }

        public void ReturnAllObjects()
        {
            ObjectPooling.ReturnAllObjects(ObjectSpawn);
        }

        public class ObjectPooling
        {
            private static List<GameObject> listPrefab = new List<GameObject>();
            private static Dictionary<GameObject, Queue<GameObject>> poolDictinary = new Dictionary<GameObject, Queue<GameObject>>();
            private static Dictionary<GameObject, Queue<GameObject>> poolDictinaryActive = new Dictionary<GameObject, Queue<GameObject>>();

            public static void CreatePool(GameObject prefab, Transform PlaceSpawn, int poolSize)
            {
                if (!poolDictinary.ContainsKey(prefab))
                {
                    listPrefab.Add(prefab);
                    poolDictinary.Add(prefab, new Queue<GameObject>());
                    poolDictinaryActive.Add(prefab, new Queue<GameObject>());
                }
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject go = GameObject.Instantiate(prefab);
                    go.name = prefab.name;
                    go.transform.SetParent(PlaceSpawn);
                    go.SetActive(false);
                    go.transform.SetParent(PlaceSpawn);
                    poolDictinary[prefab].Enqueue(go);
                }
            }

            public static GameObject GetObject(GameObject prefab, Transform PlaceSpawn)
            {
                if (!poolDictinary.ContainsKey(prefab))
                {
                    listPrefab.Add(prefab);
                    poolDictinary.Add(prefab, new Queue<GameObject>());
                    poolDictinaryActive.Add(prefab, new Queue<GameObject>());
                    GameObject goNew = GameObject.Instantiate(prefab);
                    goNew.transform.SetParent(PlaceSpawn);
                    goNew.name = prefab.name;
                    goNew.SetActive(true);
                    poolDictinaryActive[prefab].Enqueue(goNew);
                    return goNew;
                }

                if (poolDictinary[prefab].TryDequeue(out var go))
                {
                    poolDictinaryActive[prefab].Enqueue(go);
                    go.SetActive(true);
                    return go;
                }

                return NewObject(prefab, PlaceSpawn);
            }

            public static T GetObject<T>(GameObject prefab, Transform PlaceSpawn) where T : Component
            {
                if (!poolDictinary.ContainsKey(prefab))
                {
                    listPrefab.Add(prefab);
                    poolDictinary.Add(prefab, new Queue<GameObject>());
                    GameObject goNew = GameObject.Instantiate(prefab);
                    goNew.transform.SetParent(PlaceSpawn);
                    goNew.name = prefab.name;
                    goNew.SetActive(true);
                    poolDictinaryActive[prefab].Enqueue(goNew);
                    return goNew.GetComponent<T>();
                }

                if (poolDictinary[prefab].TryDequeue(out var go))
                {
                    poolDictinaryActive[prefab].Enqueue(go);
                    go.gameObject.SetActive(true);
                    return go.GetComponent<T>();
                }

                return NewObject(prefab, PlaceSpawn).GetComponent<T>();
            }

            private static GameObject NewObject(GameObject prefab, Transform PlaceSpawn)
            {
                GameObject goNew = GameObject.Instantiate(prefab);
                goNew.name = prefab.name;
                goNew.transform.SetParent(PlaceSpawn);
                goNew.SetActive(true);
                poolDictinaryActive[prefab].Enqueue(goNew);
                return goNew;
            }

            private static GameObject GetPrefab(GameObject go)
            {
                foreach (var prefab in listPrefab)
                {
                    if (go.name == prefab.name) return prefab;
                }
                return go;
            }

            public static void ReturnObject(GameObject obj)
            {
                GameObject prefab = GetPrefab(obj);
                if (!poolDictinary.ContainsKey(prefab))
                {
                    Debug.Log($"{prefab.name} not in pool" % Colorize.Red);
                    GameObject.Destroy(obj);
                    return;
                }
                obj.SetActive(false);
                poolDictinary[prefab].Enqueue(obj);
            }

            public static void ReturnAllObjects(GameObject prefab)
            {
                if (!poolDictinary.ContainsKey(prefab))
                {
                    Debug.Log($"{prefab.name} not in pool" % Colorize.Red);
                    return;
                }
                foreach (var go in poolDictinaryActive[prefab])
                {
                    go.SetActive(false);
                    poolDictinary[prefab].Enqueue(go);
                }
                poolDictinaryActive[prefab].Clear();
            }
        }
    }
}
