using HarmonyLib;
using PharloomsGlory.Modifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamCherry.Localization;
using TMProOld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PharloomsGlory.Managers;

public class PatcherManager
{
    internal static PatcherManager instance = null;

    public static void Init()
    {
        instance ??= new PatcherManager();
    }

    private Harmony harmony = null;
    public bool patched = false;

    public void Patch()
    {
        harmony ??= new Harmony("com.mars.pharloomsglory");
        if (patched)
            return;
        harmony.PatchAll();
        patched = true;
        Plugin.LogInfo("Applied Harmony patches");
    }

    [HarmonyPatch(typeof(Language))]
    [HarmonyPatch("DoSwitch")]
    public class Language_DoSwitch_Patch
    {
        private static readonly Dictionary<string, Dictionary<string, string>> entrySheets = new Dictionary<string, Dictionary<string, string>>();

        static void Postfix(LanguageCode newLang, Dictionary<string, Dictionary<string ,string>> ____currentEntrySheets)
        {
            LoadModifiedText(newLang);
            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in entrySheets)
            {
                string sheet = kvp.Key;
                if (____currentEntrySheets.ContainsKey(sheet))
                {
                    foreach (KeyValuePair<string, string> entry in kvp.Value)
                    {
                        string key = entry.Key;
                        string value = entry.Value;
                        if (____currentEntrySheets[sheet].ContainsKey(key))
                            ____currentEntrySheets[sheet][key] = value;
                        else
                            Plugin.LogError($"{key} not found in sheet {sheet}");
                    }
                }
                else
                    Plugin.LogError($"{sheet} not found in current entry sheets");
            }
            Plugin.LogInfo("Modified text");
        }

        static void LoadModifiedText(LanguageCode currentLanguage)
        {
            if (!Directory.Exists(Constants.TEXT_MODIFICATIONS_PATH))
            {
                Plugin.LogError("Failed to found text modifications directory");
                return;
            }
            entrySheets.Clear();
            foreach (string file in Directory.GetFiles(Constants.TEXT_MODIFICATIONS_PATH))
                ParseTextModifications(file, currentLanguage);
        }

        static void ParseTextModifications(string file, LanguageCode currentLanguage)
        {
            foreach (string line in File.ReadAllLines(file))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                string[] data = line.Split(':');
                if (data.Length != 4)
                {
                    Plugin.LogError($"Invalid text modification @ line {line}");
                    continue;
                }
                string code = data[0];
                if (currentLanguage != LocalizationSettings.GetLanguageEnum(code))
                    continue;
                string sheet = data[1];
                string key = data[2];
                string value = data[3];
                if (!entrySheets.ContainsKey(sheet))
                    entrySheets.Add(sheet, new Dictionary<string, string>());
                if (!entrySheets[sheet].ContainsKey(key))
                    entrySheets[sheet].Add(key, value);
                else
                    entrySheets[sheet][key] = value;
            }
        }
    }

    private Color? customHeroLightColor = null;
    private Color? customAmbientLightColor = null;
    private float? customAmbientLightIntensity = null;
    private float? customSaturation = null;

    public void SetColorValues(Color? heroLightColor, Color? ambientLightColor, float? ambientLightIntensity, float? saturation)
    {
        customHeroLightColor = heroLightColor;
        customAmbientLightColor = ambientLightColor;
        customAmbientLightIntensity = ambientLightIntensity;
        customSaturation = saturation;
        Plugin.LogInfo("Set custom color values");
    }

    public void ResetColorValues()
    {
        customHeroLightColor = null;
        customAmbientLightColor = null;
        customAmbientLightIntensity = null;
        customSaturation = null;
        Plugin.LogInfo("Reset custom color values");
    }

    [HarmonyPatch(typeof(HeroLight))]
    [HarmonyPatch("ApplyColor")]
    class HeroLight_ApplyColor_Patch
    {
        static void Prefix(HeroLight __instance, ref SpriteRenderer ___heroLightDonut, ref Color color)
        {
            if (instance.customHeroLightColor != null && SceneModifier.instance.active)
            {
                color = instance.customHeroLightColor.Value;
                ___heroLightDonut.color = color;
            }
        }
    }

    [HarmonyPatch(typeof(CustomSceneManager))]
    [HarmonyPatch("SetLighting")]
    class CustomSceneManager_SetLighting_Patch
    {
        static void Prefix(ref Color ambientLightColor, ref float ambientLightIntensity)
        {
            if (instance.customAmbientLightColor != null && SceneModifier.instance.active)
                ambientLightColor = instance.customAmbientLightColor.Value;
            if (instance.customAmbientLightIntensity != null)
                ambientLightIntensity = instance.customAmbientLightIntensity.Value;
        }
    }

    [HarmonyPatch(typeof(CustomSceneManager))]
    [HarmonyPatch("AdjustSaturation")]
    class CustomSceneManager_AdjustSaturation_Patch
    {
        static void Prefix(ref float originalSaturation)
        {
            if (instance.customSaturation != null && SceneModifier.instance.active)
                originalSaturation = instance.customSaturation.Value;
        }
    }

    private static readonly Dictionary<string, Tuple<bool, string>> wakeEffects = new Dictionary<string, Tuple<bool, string>>();

    public void SetWakeEffects(List<SceneModifier.DeactivateWakeEffectData> parsedDeactivatedWakeEffects)
    {
        wakeEffects.Clear();
        foreach (SceneModifier.DeactivateWakeEffectData data in parsedDeactivatedWakeEffects)
        {
            if (!wakeEffects.ContainsKey(data.range))
                wakeEffects.Add(data.range, new Tuple<bool, string>(false, data.effect));
        }
    }

    [HarmonyPatch(typeof(AlertRange))]
    [HarmonyPatch("Update")]
    class AlertRange_Update_Patch
    {
        private static readonly int MAX_ITERATIONS = 10;

        static void Postfix(AlertRange __instance)
        {
            if (__instance.IsHeroInRange() && __instance.GetUnalertTime() == 0f)
            {
                string uniqueName = Util.GetGameObjectUniqueName(__instance.gameObject);
                if (wakeEffects.ContainsKey(uniqueName) && !wakeEffects[uniqueName].Item1)
                {
                    GameManager.instance.StartCoroutine(DeactivateEffect(uniqueName));
                    wakeEffects[uniqueName] = new Tuple<bool, string>(true, wakeEffects[uniqueName].Item2);
                }
            }
        }

        static IEnumerator DeactivateEffect(string uniqueName)
        {
            if (!wakeEffects.ContainsKey(uniqueName))
                yield break;
            Dictionary<string, GameObject> gameObjects;
            Scene currentScene = SceneManager.GetActiveScene();
            int iterations = 0;
            do
            {
                gameObjects = Util.GetAllGameObjectsAsDictionary(currentScene, true);
                iterations++;
                yield return null;
            } while (iterations < MAX_ITERATIONS || !gameObjects.ContainsKey(wakeEffects[uniqueName].Item2));
            GameObject effect = gameObjects[wakeEffects[uniqueName].Item2];
            effect?.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(SaveSlotBackgrounds))]
    [HarmonyPatch("Awake")]
    class SaveSlotBackground_Awake_Patch
    {
        static void Postfix(SaveSlotBackgrounds __instance, SaveSlotBackgrounds.AreaBackground[] ___areaBackgrounds, SaveSlotBackgrounds.AreaBackground[] ___extraAreaBackgrounds)
        {
            if (!Directory.Exists(Constants.TEXTURES_PATH))
            {
                Plugin.LogError("Failed to find updated textures directory");
                return;
            }
            string areaArtTexturesPath = Path.Combine(Constants.TEXTURES_PATH, Constants.AREA_ART_TEXTURES_DIRECTORY);
            if (!Directory.Exists(areaArtTexturesPath))
            {
                Plugin.LogError("Failed to find area art textures directory");
                return;
            }
            Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
            foreach (string dir in Directory.GetDirectories(areaArtTexturesPath))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    try
                    {
                        Texture2D t = new Texture2D(2, 2);
                        if (!t.LoadImage(File.ReadAllBytes(file)))
                        {
                            Plugin.LogError($"Failed to load sprite texture {file}");
                            continue;
                        }
                        textures.TryAdd(Path.GetFileNameWithoutExtension(file), t);
                    }
                    catch (Exception e)
                    {
                        Plugin.LogError($"Exception while loading updated sprite textures: {e.Message}");
                    }
                }
            }

            foreach (SaveSlotBackgrounds.AreaBackground bg in ___areaBackgrounds.Concat(___extraAreaBackgrounds))
            {
                if (bg?.BackgroundImage?.texture == null)
                    continue;
                string spriteName = Util.GetSpriteName(bg.BackgroundImage);
                if (!textures.ContainsKey(spriteName))
                    continue;
                Texture2D t = textures[spriteName];
                float spriteWidth = bg.BackgroundImage.textureRect.width;
                float spriteHeight = bg.BackgroundImage.textureRect.height;
                Vector2 spritePivot = bg.BackgroundImage.pivot;
                spritePivot.x /= spriteWidth;
                spritePivot.y /= spriteHeight;
                Sprite updatedSprite = Sprite.Create(
                    t,
                    new Rect(2, 2, t.width - 2, t.height - 2),
                    new Vector2(0.5f, 0.5f),
                    bg.BackgroundImage.pixelsPerUnit, 0, SpriteMeshType.FullRect,
                    Vector4.zero
                );
                bg.BackgroundImage = updatedSprite;
                bg.Act3BackgroundImage = updatedSprite;
            }
        }
    }

    [HarmonyPatch(typeof(AudioSource))]
    [HarmonyPatch("PlayHelper", [typeof(AudioSource), typeof(ulong)])]
    class AudioSource_PlayHelper_Patch
    {
        static void Prefix(AudioSource source)
        {
            if (source?.clip?.name != null && SceneModifier.instance.active && AudioModifier.instance.GetAudioClip(source.clip.name, out AudioClip clip))
                source.clip = clip;
        }
    }

    [HarmonyPatch(typeof(AudioSource))]
    [HarmonyPatch("PlayOneShotHelper", [typeof(AudioSource), typeof(AudioClip), typeof(float)])]
    class AudioSource_PlayOneShotHelper_Patch
    {
        static void Prefix(AudioSource source, ref AudioClip clip)
        {
            if (source?.clip?.name != null && clip != null && SceneModifier.instance.active && AudioModifier.instance.GetAudioClip(source.clip.name, out AudioClip updatedClip))
                clip = updatedClip;
        }
    }

    [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.clip), MethodType.Setter)]
    class AudioSource_AudioClipSetter_Patch
    {
        static void Postfix(AudioSource __instance)
        {
            if (__instance?.clip?.name != null && SceneModifier.instance.active && AudioModifier.instance.GetAudioClip(__instance.clip.name, out AudioClip clip))
                __instance.resource = clip;
        }
    }

    [HarmonyPatch(typeof(InventoryItemWideMapZone))]
    [HarmonyPatch("UpdateColor")]
    class InventoryItemWideMapZone_UpdateColor_Patch
    {
        static void Prefix(InventoryItemWideMapZone __instance, ref Color ___initialColor)
        {
            if (__instance.ZoomToZone == GlobalEnums.MapZone.JUDGE_STEPS)
                ___initialColor = Constants.BLASTED_STEPS_MAP_COLOR;
        }
    }

    private static readonly string[] BLASTED_STEPS_SCENES =
    [
        "Coral_19_base",
        "Bellway_08",
        "Coral_02",
        "Coral_03",
        "Coral_11",
        "Coral_12",
        "Coral_19",
        "Coral_19_left",
        "Coral_19b",
        "Coral_32",
        "Coral_32_arrow",
        "Coral_33",
        "Coral_34",
        "Coral_34_mid",
        "Coral_34_top",
        "Coral_11b",
        "Coral_35",
        "Coral_35_secret",
        "Coral_36",
        "Coral_35_top_break_base_bottom",
        "Coral_37",
        "Coral_37_join",
        "Coral_42",
        "Coral_42_top",
        "Coral_43",
        "Coral_Judge_Arena"
    ];

    [HarmonyPatch(typeof(GameMapScene))]
    [HarmonyPatch("GetColor")]
    class GameMapScene_GetColor_Patch
    {
        static void Postfix(GameMapScene __instance, ref Color __result)
        {
            if (BLASTED_STEPS_SCENES.Contains(__instance.Name))
                __result = Constants.BLASTED_STEPS_MAP_COLOR;
        }
    }

    [HarmonyPatch(typeof(GameMapScene))]
    [HarmonyPatch("OnAwake")]
    class GameMapScene_OnAwake_Patch
    {
        static void Postfix(GameMapScene __instance, ref SpriteRenderer ___spriteRenderer)
        {
            if (!BLASTED_STEPS_SCENES.Contains(__instance.Name))
                return;
            ___spriteRenderer.color = Constants.BLASTED_STEPS_MAP_COLOR;

        }
    }

    [HarmonyPatch(typeof(GameMap.ZoneInfo))]
    [HarmonyPatch("GetComponents")]
    class GameMap_ZoneInfo_GetComponents_Patch
    {
        static void Postfix(GameMap.ZoneInfo __instance)
        {
            if (!__instance.Parents.Any(parent => parent.HasParent && parent.Parent.name == "Blasted_Steps"))
                return;
            GameMap.ParentInfo parent = __instance.Parents.First(parent => parent.HasParent && parent.Parent.name == "Blasted_Steps");
            if (parent == null)
                return;
            GameObject parentObject = parent.Parent;
            foreach (TextMeshPro tmp in parentObject.GetComponentsInChildren<TextMeshPro>())
            {
                if (tmp.gameObject.name == "Area Name (1)")
                {
                    tmp.color = Constants.BLASTED_STEPS_MAP_COLOR;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(SurfaceWaterRegion))]
    [HarmonyPatch("Awake")]
    class SurfaceWaterRegion_Awake_Patch
    {
        static Exception Finalizer()
        {
            return null;
        }
    }
}
