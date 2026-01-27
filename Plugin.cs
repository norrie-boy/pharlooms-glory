using BepInEx;
using PharloomsGlory.Managers;
using PharloomsGlory.Modifiers;
using UnityEngine.SceneManagement;

namespace PharloomsGlory;

[BepInPlugin(Constants.PLUGIN_GUID, Constants.PLUGIN_NAME, Constants.PLUGIN_VERSION)]
[BepInDependency("org.silksong-modding.assethelper")]
public class Plugin : BaseUnityPlugin
{
    internal static Plugin instance;

    private void Awake()
    {
        instance = this;
        Logger.LogInfo($"[{Constants.PLUGIN_NAME} {Constants.PLUGIN_VERSION}] has loaded");

        SpriteRendererModifier.Init();
        TK2DSpriteModifier.Init();
        AudioModifier.Init();
        SceneModifier.Init();
        PatcherManager.Init();
        AssetManager.Init();

        SpriteRendererModifier.instance.LoadTextures();
        AudioModifier.instance.LoadAudioClips();

        AssetManager.instance.RequestAssets();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _mode)
    {
        if (!PatcherManager.instance.patched)
            PatcherManager.instance.Patch();
        if (!AssetManager.instance.IsLoaded)
            AssetManager.instance.LoadAllAssets();

        SceneModifier.instance.Modify(scene);
    }

    public static void LogInfo(string msg)
    {
        instance.Logger.LogInfo(msg);
    }

    public static void LogError(string msg)
    {
        instance.Logger.LogError(msg);
    }
}
