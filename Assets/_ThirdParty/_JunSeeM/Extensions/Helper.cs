using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JunEngine
{
    public class Helper
    {
        public static bool IsPointerOverGameObject()
        {
            //check mouse
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true;

            //check touch
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return true;
            }

            return false;
        }

        public static bool IsLayerInMask(LayerMask layerMask, int layer)
        {
            return ((1 << layer) & layerMask) != 0;
        }

        public static void SaveData<T>(T data, string namePath)
        {
            string json = JsonUtility.ToJson(data);
            string filePath = Application.persistentDataPath + "/" + namePath + ".json";
            Debug.Log(filePath);
            File.WriteAllText(filePath, json);
        }

        public static T LoadData<T>(string namePath)
        {
            string filePath = Application.persistentDataPath + "/" + namePath + ".json";
            if (File.Exists(filePath))
            {
                string loadedJson = File.ReadAllText(filePath);

                T loadedPlayerData = JsonUtility.FromJson<T>(loadedJson);
                return loadedPlayerData;
            }
            else
            {
                Debug.LogWarning("File not found: " + filePath);
            }
            return default(T);
        }

        public static void SaveDataPlayerPrefs<T>(T data, string path)
        {
            string json = JsonUtility.ToJson(data);
            Debug.Log($"save data {path}: {json}");
            //File.WriteAllText(filePath, json);
            PlayerPrefs.SetString(path, json);
        }

        public static T LoadDataPlayerPrefs<T>(string path)
        {
            if (PlayerPrefs.HasKey(path))
            {
                string json = PlayerPrefs.GetString(path);
                Debug.Log($"Load data {path}: {json}");
                return JsonUtility.FromJson<T>(json);
            }
            return default(T);
        }

        public static Vector3 ApplyRotationVector(Vector3 vec, float angle)
        {
            return Quaternion.Euler(0f, 0f, angle) * vec;
        }

        public static IEnumerator OnDelayFunc(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        public static int GetFirstLayerFromMask(LayerMask mask)
        {
            int layer = 0;
            int maskValue = mask.value;
            while (maskValue > 0)
            {
                if ((maskValue & 1) == 1)
                {
                    return layer;
                }
                maskValue >>= 1;
                layer++;
            }
            return -1; // No valid layer found
        }

        public static void ShuffleList<T>(IList<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
