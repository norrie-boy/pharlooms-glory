using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TeamCherry.NestedFadeGroup;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PharloomsGlory.Modifiers;

public class SpriteRendererModifier
{
    internal static SpriteRendererModifier instance = null;

    public static void Init()
    {
        instance ??= new SpriteRendererModifier();
    }

    private readonly Dictionary<GlobalEnums.MapZone, Dictionary<string, Texture2D>> spriteTextures = new Dictionary<GlobalEnums.MapZone, Dictionary<string, Texture2D>>();
    private readonly Dictionary<GlobalEnums.MapZone, Dictionary<string, Sprite>> updatedSprites = new Dictionary<GlobalEnums.MapZone, Dictionary<string, Sprite>>();
    private readonly Dictionary<GlobalEnums.MapZone, Dictionary<string, Vector2>> pivotOffsets = new Dictionary<GlobalEnums.MapZone, Dictionary<string, Vector2>>();

    private GlobalEnums.MapZone? overrideMapZone;
    
    private readonly Dictionary<string, Texture2D> hudSpriteTextures = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Sprite> updatedHudSprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Vector2> hudPivotOffsets = new Dictionary<string, Vector2>();

    private readonly string[] VALID_HUD_OBJECTS =
    [
        "Wide Map(Clone)",
        "Shop Item Template(Clone)"
    ];

    private readonly List<SpriteRenderer> loadedSpriteRenderers = new List<SpriteRenderer>();

    public void LoadTextures()
    {
        LoadPivotOffsets();
        LoadHUDPivotOffsets();
        spriteTextures.Clear();
        hudSpriteTextures.Clear();
        if (!Directory.Exists(Constants.TEXTURES_PATH))
        {
            Plugin.LogError("Failed to find updated textures directory");
            return;
        }
        foreach (string dir in Directory.GetDirectories(Constants.TEXTURES_PATH))
        {
            string dirName = Path.GetFileName(dir);
            if (dirName == Constants.HUD_TEXTURES_DIRECTORY)
                LoadHUDTextures(dir);
            else
            {
                GlobalEnums.MapZone mapZone = StringToMapZone(dirName);
                if (mapZone != GlobalEnums.MapZone.NONE)
                    LoadAreaTextures(dir, mapZone);
            }
        }
    }

    private void LoadPivotOffsets()
    {
        pivotOffsets.Clear();
        foreach (string dir in Directory.GetDirectories(Constants.TEXTURES_PATH))
        {
            GlobalEnums.MapZone mapZone = StringToMapZone(Path.GetFileName(dir));
            if (mapZone == GlobalEnums.MapZone.NONE)
                continue;
            string pivotOffsetsFilePath = Path.Combine(dir, Constants.SPRITE_OFFSETS_FILE_NAME);
            if (!File.Exists(pivotOffsetsFilePath))
                continue;
            foreach (string line in File.ReadAllLines(pivotOffsetsFilePath))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                // Format: <SpriteName>:<OffsetX>,<OffsetY> (in pixels)
                string[] data = line.Split(":");
                if (data.Length != 2)
                {
                    Plugin.LogError($"Invalid sprite offset @ line {line}");
                    continue;
                }
                string spriteName = data[0];
                string[] coords = data[1].Split(",");
                if (!float.TryParse(coords[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) || !float.TryParse(coords[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float y))
                {
                    Plugin.LogError($"Invalid sprite offset @ line {line}");
                    continue;
                }
                Vector2 offset = new Vector2(-x, -y);
                if (!pivotOffsets.ContainsKey(mapZone))
                    pivotOffsets.Add(mapZone, new Dictionary<string, Vector2>());
                if (!pivotOffsets[mapZone].TryAdd(spriteName, offset))
                    Plugin.LogError($"Failed to add sprite offset {offset} ({spriteName}) as it was already defined");
            }
        }
    }

    private void LoadHUDPivotOffsets()
    {
        hudPivotOffsets.Clear();
        string pivotOffsetsFilePath = Path.Combine(Constants.TEXTURES_PATH, Constants.HUD_TEXTURES_DIRECTORY, Constants.SPRITE_OFFSETS_FILE_NAME);
        if (!File.Exists(pivotOffsetsFilePath))
            return;
        foreach (string line in File.ReadAllLines(pivotOffsetsFilePath))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            string[] data = line.Split(":");
            if (data.Length != 2)
            {
                Plugin.LogError($"Invalid hud sprite offset @ line {line}");
                continue;
            }
            string spriteName = data[0];
            string[] coords = data[1].Split(",");
            if (!float.TryParse(coords[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) || !float.TryParse(coords[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float y))
            {
                Plugin.LogError($"Invalid sprite hud offset @ line {line}");
                continue;
            }
            Vector2 offset = new Vector2(-x, -y);
            if (!hudPivotOffsets.TryAdd(spriteName, offset))
                Plugin.LogError($"Failed to add hud sprite offset {offset} ({spriteName}) as it was already defined");
        }
    }

    private void LoadHUDTextures(string dir)
    {
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            foreach (string file in Directory.GetFiles(subDir))
            {
                try
                {
                    Texture2D t = new Texture2D(2, 2);
                    if (!t.LoadImage(File.ReadAllBytes(file)))
                    {
                        Plugin.LogError($"Failed to load HUD texture {file}");
                        continue;
                    }
                    if (!hudSpriteTextures.TryAdd(Path.GetFileNameWithoutExtension(file), t))
                        Plugin.LogError($"Failed to load HUD texture {file} as it was already loaded");
                }
                catch (Exception e)
                {
                    Plugin.LogError($"Exception while loading HUD texture: {e.Message}");
                }
            }
        }
        Plugin.LogInfo("Loaded HUD textures");
    }

    private void LoadAreaTextures(string dir, GlobalEnums.MapZone mapZone)
    {
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            foreach (string file in Directory.GetFiles(subDir))
            {
                try
                {
                    Texture2D t = new Texture2D(2, 2);
                    if (!t.LoadImage(File.ReadAllBytes(file)))
                    {
                        Plugin.LogError($"Failed to load sprite texture {file}");
                        continue;
                    }
                    if (!spriteTextures.ContainsKey(mapZone))
                        spriteTextures.Add(mapZone, new Dictionary<string, Texture2D>());
                    if (!spriteTextures[mapZone].TryAdd(Path.GetFileNameWithoutExtension(file), t))
                        Plugin.LogError($"Failed to load sprite texture {file} as it was already loaded");
                }
                catch (Exception e)
                {
                    Plugin.LogError($"Exception while loading sprite texture: {e.Message}");
                }
            }
        }
        Plugin.LogInfo($"Loaded sprite textures for {mapZone}");
    }

    private GlobalEnums.MapZone StringToMapZone(string s)
    {
        return s switch
        {
            "JUDGE_STEPS" => GlobalEnums.MapZone.JUDGE_STEPS,
            "CORAL_CAVERNS" => GlobalEnums.MapZone.CORAL_CAVERNS,
            _ => GlobalEnums.MapZone.NONE
        };
    }

    private void LoadSpriteRenderers(Scene scene)
    {
        loadedSpriteRenderers.Clear();
        foreach (SpriteRenderer sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
        {
            if (sr?.sprite?.texture == null)
                continue;
            if (sr.gameObject.scene == scene || IsInHUD(sr))
                loadedSpriteRenderers.AddIfNotPresent(sr);
        }
    }

    private bool IsInHUD(SpriteRenderer sr)
    {
        Transform parent = sr.gameObject.transform.parent;
        while (parent != null)
        {
            if (parent == GameCameras.instance.hudCamera.transform || VALID_HUD_OBJECTS.Contains(parent.name))
                return true;
            parent = parent.parent;
        }
        return false;
    }

    public void LoadSpriteRenderersWithIgnores(Scene scene, List<string> ignoredObjects, List<SceneModifier.SpriteAlterPointData> ignoredPoints)
    {
        LoadSpriteRenderers(scene);

        loadedSpriteRenderers.RemoveAll(sr =>
        {
            string uniqueName = Util.GetGameObjectUniqueName(sr.gameObject);
            if (ignoredObjects.Contains(uniqueName))
                return true;
            foreach (SceneModifier.SpriteAlterPointData data in ignoredPoints)
            {
                if (Vector2.Distance(sr.gameObject.transform.position, data.position) <= data.radius)
                    return true;
            }
            return false;
        });
    }

    public void SetMapZone(GlobalEnums.MapZone? mapZone)
    {
        overrideMapZone = mapZone;
    }

    public void UpdateLoaded()
    {
        ModifyLoadedSpriteRenderers();
        ModifyHUDSpriteRenderers();
        ModifyShopItemSpriteRenderers();
    }

    private void ModifyLoadedSpriteRenderers()
    {
        foreach (SpriteRenderer sr in loadedSpriteRenderers)
        {
            if (sr?.sprite?.texture == null)
                continue;
            ModifySpriteRenderer(sr, overrideMapZone);
        }
    }

    public void ModifySpriteRenderer(SpriteRenderer sr, GlobalEnums.MapZone? mapZone = null, string? overrideSpriteName = null)
    {
        GlobalEnums.MapZone currentMapZone = GameManager.instance.sm?.mapZone ?? GlobalEnums.MapZone.NONE;
        GlobalEnums.MapZone selectedMapZone = mapZone ?? overrideMapZone ?? currentMapZone;
        if (!spriteTextures.ContainsKey(selectedMapZone))
            return;
        string spriteName = overrideSpriteName ?? Util.GetSpriteName(sr.sprite);
        if (!GetSprite(spriteTextures[selectedMapZone],
            updatedSprites.GetValueOrDefault(selectedMapZone, new Dictionary<string, Sprite> { }),
            pivotOffsets.GetValueOrDefault(selectedMapZone, new Dictionary<string, Vector2> { }),
            sr.sprite, spriteName, out Sprite s))
            return;
        if (!updatedSprites.ContainsKey(selectedMapZone))
            updatedSprites.Add(selectedMapZone, new Dictionary<string, Sprite>());
        if (!updatedSprites[selectedMapZone].ContainsKey(spriteName))
            updatedSprites[selectedMapZone].Add(spriteName, s);
        sr.sprite = s;
    }

    private bool GetSprite(Dictionary<string, Texture2D> textures, Dictionary<string, Sprite> sprites, Dictionary<string, Vector2> offsets, Sprite original, string spriteName, out Sprite sprite)
    {
        sprite = null;
        if (sprites.TryGetValue(spriteName, out Sprite s))
        {
            sprite = s;
            return true;
        }
        if (!textures.TryGetValue(spriteName, out Texture2D t))
            return false;
        float spriteWidth = original.textureRect.width;
        float spriteHeight = original.textureRect.height;
        Vector2 spritePivot = original.pivot;
        spritePivot.x /= spriteWidth;
        spritePivot.y /= spriteHeight;
        Vector2 pivotOffset = offsets.GetValueOrDefault(spriteName, Vector2.zero);
        pivotOffset.x /= spriteWidth;
        pivotOffset.y /= spriteHeight;
        Sprite updatedSprite = Sprite.Create(
            t,
            new Rect(0, 0, t.width, t.height),
            spritePivot + pivotOffset,
            original.pixelsPerUnit, 0, SpriteMeshType.FullRect,
            Vector4.zero
        );
        sprite = updatedSprite;
        return true;
    }

    private void ModifyHUDSpriteRenderers()
    {
        foreach (SpriteRenderer sr in loadedSpriteRenderers)
        {
            if (IsInHUD(sr))
                ModifyHUDSpriteRenderer(sr);
        }
    }

    private void ModifyHUDSpriteRenderer(SpriteRenderer sr)
    {
        string spriteName = Util.GetSpriteName(sr.sprite);
        if (!GetSprite(hudSpriteTextures, updatedHudSprites, hudPivotOffsets, sr.sprite, spriteName, out Sprite s))
            return;
        if (!updatedHudSprites.ContainsKey(spriteName))
            updatedHudSprites.Add(spriteName, s);
        sr.sprite = s;

        NestedFadeGroupSpriteRenderer nfgsr = sr.gameObject.GetComponent<NestedFadeGroupSpriteRenderer>();
        nfgsr?.Sprite = s;
        nfgsr?.spriteRenderer?.sprite = s;
    }

    private void ModifyShopItemSpriteRenderers()
    {
        foreach (ShopItem item in Resources.FindObjectsOfTypeAll<ShopItem>())
        {
            if (!GetSprite(hudSpriteTextures, updatedHudSprites, hudPivotOffsets, item.ItemSprite, Util.GetSpriteName(item.ItemSprite), out Sprite s))
                continue;
            FieldInfo itemSpriteFieldInfo = typeof(ShopItem).GetField("itemSprite", BindingFlags.NonPublic | BindingFlags.Instance);
            if (itemSpriteFieldInfo == null)
            {
                Plugin.LogError("Failed to get itemSprite field info");
                continue;
            }
            itemSpriteFieldInfo.SetValue(item, s);
        }
    }

    public void UpdateOnly(List<SceneModifier.SpriteModifyObjectData> selectedObjects)
    {
        ModifySelectedSprites(selectedObjects);
        ModifyHUDSpriteRenderers();
        ModifyShopItemSpriteRenderers();
    }

    private void ModifySelectedSprites(List<SceneModifier.SpriteModifyObjectData> selectedObjects)
    {
        Dictionary<string, GlobalEnums.MapZone?> selectedObjectsDictionary = selectedObjects.ToDictionary(
            data => data.name, data => data.mapZone
        );
        foreach (SpriteRenderer sr in loadedSpriteRenderers)
        {
            if (sr?.sprite?.texture == null)
                continue;
            string objectName = Util.GetGameObjectUniqueName(sr.gameObject);
            if (selectedObjectsDictionary.ContainsKey(objectName))
                ModifySpriteRenderer(sr, selectedObjectsDictionary[objectName]);
        }
    }

    public void UpdatePoint(SceneModifier.SpriteAlterPointData data)        // TODO: optimize
    {
        List<SpriteRenderer> updatedSpriteRenderers = new List<SpriteRenderer>();
        foreach (SpriteRenderer sr in loadedSpriteRenderers)
        {
            if (sr?.sprite == null)
                continue;
            if (Vector2.Distance(sr.gameObject.transform.position, data.position) <= data.radius)
            {
                ModifySpriteRenderer(sr, data.mapZone);
                updatedSpriteRenderers.AddIfNotPresent(sr);
            }
        }
        loadedSpriteRenderers.RemoveAll(sr => updatedSpriteRenderers.Contains(sr));
    }

    public bool GetTexture(string textureName, out Texture2D texture, GlobalEnums.MapZone? mapZone = null)
    {
        GlobalEnums.MapZone currentMapZone = GameManager.instance.sm?.mapZone ?? GlobalEnums.MapZone.NONE;
        GlobalEnums.MapZone selectedMapZone = mapZone ?? overrideMapZone ?? currentMapZone;
        if (!spriteTextures.ContainsKey(selectedMapZone))
        {
            texture = null;
            return false;
        }
        return spriteTextures[selectedMapZone].TryGetValue(textureName, out texture);
    }
}
