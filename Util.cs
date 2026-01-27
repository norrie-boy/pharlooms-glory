using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PharloomsGlory;

public class Util
{
    private static readonly List<GameObject> gameObjects = new List<GameObject>();
    private static readonly Dictionary<string, GameObject> gameObjectsDictionary = new Dictionary<string, GameObject>();
    private static string currentSceneName = string.Empty;
    private static string currentDictionarySceneName = string.Empty;

    public static string GetValidTextureName(string name)
    {
        return name.Replace("|", "-");
    }

    public static string GetSpriteName(Sprite sprite)
    {
        if (sprite?.texture?.name == null || sprite?.textureRect == null)
            return string.Empty;
        string x = ((int)sprite.textureRect.x).ToString();
        string y = ((int)sprite.textureRect.y).ToString();
        string width = ((int)sprite.textureRect.width).ToString();
        string height = ((int)sprite.textureRect.height).ToString();
        return $"{GetValidTextureName(sprite.texture.name)}-sprite-x{x}y{y}w{width}h{height}";
    }

    public static List<GameObject> GetAllGameObjects(Scene scene, bool forceReload = false)
    {
        if (!scene.IsValid())
        {
            Plugin.LogError("Failed to get scene game objects because the scene was invalid");
            gameObjects.Clear();
            return gameObjects;
        }
        if (!forceReload && scene.name == currentSceneName)
            return gameObjects;
        gameObjects.Clear();
        currentSceneName = scene.name;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
            {
                if (t?.gameObject != null)
                    gameObjects.AddIfNotPresent(t.gameObject);
            }
        }
        return gameObjects;
    }

    public static string GetGameObjectUniqueName(GameObject go)
    {
        string x = ((int)go.transform.GetPositionX()).ToString();
        string y = ((int)go.transform.GetPositionY()).ToString();
        return $"{go.name}-x{x}y{y}";
    }

    public static Dictionary<string, GameObject> GetAllGameObjectsAsDictionary(Scene scene, bool forceReload = false)
    {
        if (!scene.IsValid())
        {
            Plugin.LogError("Failed to get scene game objects as dictionary because the scene is invalid");
            gameObjectsDictionary.Clear();
            return gameObjectsDictionary;
        }
        if (!forceReload && scene.name == currentDictionarySceneName)
            return gameObjectsDictionary;
        gameObjectsDictionary.Clear();
        currentDictionarySceneName = scene.name;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
            {
                if (t?.gameObject != null)
                {
                    string uniqueName = GetGameObjectUniqueName(t.gameObject);
                    if (!gameObjectsDictionary.ContainsKey(uniqueName))
                        gameObjectsDictionary.Add(uniqueName, t.gameObject);
                }
            }
        }
        return gameObjectsDictionary;
    }

    public static void Clear()
    {
        currentSceneName = string.Empty;
        gameObjects.Clear();
        currentDictionarySceneName = string.Empty;
        gameObjectsDictionary.Clear();
    }
}
