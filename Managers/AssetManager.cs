using Silksong.AssetHelper.ManagedAssets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PharloomsGlory.Managers;

public class AssetManager
{
    internal static AssetManager instance = null;

    public static void Init()
    {
        instance ??= new AssetManager();
    }

    private readonly Dictionary<string, List<string>> OBJECT_PATHS = new Dictionary<string, List<string>>();
    private readonly Dictionary<string, List<string>> OBJECT_RENAMES = new Dictionary<string, List<string>>();
    public struct RiverCloneData
    {
        public enum ComponentType
        {
            NONE,
            MESH_RENDERER
        }
        public enum ResourceType
        {
            NONE,
            TEXTURE,
            SHADER
        }
        public ComponentType component;
        public ResourceType resourceType;
        public string resourcePath;
    }
    private readonly Dictionary<string, Dictionary<string, List<RiverCloneData>>> RIVER_CLONES_DATA = new Dictionary<string, Dictionary<string, List<RiverCloneData>>>();
    private readonly Dictionary<string, ManagedAsset<GameObject>> LOADED_ASSETS = new Dictionary<string, ManagedAsset<GameObject>>();

    private bool didRequest = false;

    public void RequestAssets()
    {
        if (didRequest)
            return;
        LoadObjectPaths();
        LoadObjectRenames();
        LoadRiverClonesData();
        foreach (string key in OBJECT_PATHS.Keys)
            RequestAssetsFromScene(key);
        didRequest = true;
    }

    private void LoadObjectPaths()
    {
        if (!File.Exists(Constants.OBJECT_PATHS_FILE_PATH))
        {
            Plugin.LogError("Failed to find valid clone objects file");
            return;
        }
        foreach (string line in File.ReadAllLines(Constants.OBJECT_PATHS_FILE_PATH))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            string[] pair = line.Split(":");
            string sceneName = pair[0];
            string objectPath = pair[1];
            if (OBJECT_PATHS.ContainsKey(sceneName))
                OBJECT_PATHS[sceneName].Add(objectPath);
            else
                OBJECT_PATHS.Add(sceneName, new List<string> { objectPath });
        }
    }

    private void LoadObjectRenames()
    {
        if (!File.Exists(Constants.OBJECT_RENAMES_FILE_PATH))
        {
            Plugin.LogError("Failed to find object renames file");
            return;
        }
        foreach (string line in File.ReadAllLines(Constants.OBJECT_RENAMES_FILE_PATH))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            string[] pair = line.Split(":");
            string objectPath = pair[0];
            string rename = pair[1];
            if (OBJECT_RENAMES.ContainsKey(rename))
                OBJECT_RENAMES[rename].AddIfNotPresent(objectPath);
            else if (!OBJECT_RENAMES.TryAdd(rename, new List<string> { objectPath }))
                Plugin.LogError($"Failed to add rename {rename}");
        }
    }

    private void LoadRiverClonesData()
    {
        if (!File.Exists(Constants.RIVER_CLONES_DATA_FILE_PATH))
        {
            Plugin.LogError("Failed to find river clones data file");
            return;
        }
        foreach (string line in File.ReadAllLines(Constants.RIVER_CLONES_DATA_FILE_PATH))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            // format: sceneName:path:component:resourceType:resourcePath
            string[] data = line.Split(":");
            if (data.Length != 5)
            {
                Plugin.LogError($"Failed to parse river data @ line {line}");
                continue;
            }
            string sceneName = data[0];
            string path = data[1];
            string component = data[2];
            string resourceType = data[3];
            string resourcePath = data[4];
            if (!RIVER_CLONES_DATA.ContainsKey(sceneName))
                RIVER_CLONES_DATA.Add(sceneName, new Dictionary<string, List<RiverCloneData>>());
            if (!RIVER_CLONES_DATA[sceneName].ContainsKey(path))
                RIVER_CLONES_DATA[sceneName].Add(path, new List<RiverCloneData>());
            RIVER_CLONES_DATA[sceneName][path].Add(new RiverCloneData()
            {
                component = component switch
                {
                    "MeshRenderer" => RiverCloneData.ComponentType.MESH_RENDERER,
                    _ => RiverCloneData.ComponentType.NONE
                },
                resourceType = resourceType switch
                {
                    "Texture" => RiverCloneData.ResourceType.TEXTURE,
                    "Shader" => RiverCloneData.ResourceType.SHADER,
                    _ => RiverCloneData.ResourceType.NONE
                },
                resourcePath = resourcePath
            });
        }
    }

    private void RequestAssetsFromScene(string sceneName)
    {
        if (!OBJECT_PATHS.TryGetValue(sceneName, out List<string> objectPaths))
            return;
        foreach (string objectPath in objectPaths)
        {
            ManagedAsset<GameObject> asset = ManagedAsset<GameObject>.FromSceneAsset(sceneName, objectPath);
            if (!LOADED_ASSETS.TryAdd(objectPath, asset))
            {
                Plugin.LogError($"Failed to request asset {objectPath}");
                continue;
            }
        }
        Plugin.LogInfo($"Requested assets from {sceneName}");
    }

    private bool isLoaded = false;

    public bool IsLoaded => isLoaded;

    public void LoadAllAssets()
    {
        if (isLoaded)
            return;
        GameManager gm = GameManager.instance;
        if (gm == null)
            return;
        gm.StartCoroutine(LoadAllAssetsRoutine());
    }

    private IEnumerator LoadAllAssetsRoutine()
    {
        List<ManagedAsset<GameObject>> assets = new List<ManagedAsset<GameObject>>();
        foreach (ManagedAsset<GameObject> asset in LOADED_ASSETS.Values)
        {
            asset.Load();
            assets.Add(asset);
        }
        yield return new WaitUntil(() => assets.All(asset => asset.IsLoaded));
        foreach (ManagedAsset<GameObject> asset in assets)
        {
            if (asset.Handle.OperationException != null)
                Plugin.LogError($"Error loading asset {asset.Key}: {asset.Handle.OperationException.Message}");
        }
        isLoaded = true;
    }

    public bool GetAsset(string name, out ManagedAsset<GameObject> asset)
    {
        string path = !OBJECT_RENAMES.ContainsKey(name) ? name : OBJECT_RENAMES[name].GetRandomElement();
        return LOADED_ASSETS.TryGetValue(path, out asset);
    }

    public string GetPathFromRename(string rename) => OBJECT_RENAMES[rename].GetRandomElement();

    public Dictionary<string, Dictionary<string, List<RiverCloneData>>>.ValueCollection GetRiverClonesData() => RIVER_CLONES_DATA.Values;
}
