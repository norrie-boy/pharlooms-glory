using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PharloomsGlory.Modifiers;

public class TK2DSpriteModifier
{
    internal static TK2DSpriteModifier instance = null;

    public static void Init()
    {
        instance ??= new TK2DSpriteModifier();
    }

    private readonly List<tk2dSpriteCollectionData> loadedSpriteCollections = new List<tk2dSpriteCollectionData>();

    public void LoadSpriteCollections()
    {
        loadedSpriteCollections.Clear();
        foreach (tk2dSpriteCollectionData sc in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
            loadedSpriteCollections.AddIfNotPresent(sc);
    }

    public void UpdateLoaded()
    {
        ModifyLoadedSpriteCollections();
    }

    private void ModifyLoadedSpriteCollections()
    {
        foreach (tk2dSpriteCollectionData sc in loadedSpriteCollections)
        {
            ModifySpriteCollection(sc);
        }
    }

    public void ModifySpriteCollection(tk2dSpriteCollectionData sc)
    {
        if (sc?.materials == null)
        {
            if (sc?.material?.mainTexture != null && GetTexture(sc.name, sc.material.mainTexture.name, out Texture2D t))
                sc.material.mainTexture = t;
            return;
        }
        foreach (Material m in sc.materials)
        {
            if (m?.mainTexture == null)
                continue;
            if (GetTexture(sc.name, m.mainTexture.name, out Texture2D t))
                m.mainTexture = t;
        }
    }

    private bool GetTexture(string scName, string atlasName, out Texture2D texture)
    {
        texture = null;
        if (!Directory.Exists(Constants.TEXTURES_PATH))
        {
            Plugin.LogError("Failed to find updated textures directory");
            return false;
        }
        string tk2dTexturesPath = Path.Combine(Constants.TEXTURES_PATH, Constants.TK2D_TEXTURES_DIRECTORY);
        if (!Directory.Exists(tk2dTexturesPath))
        {
            Plugin.LogError("Failed to find tk2d textures directory");
            return false;
        }
        foreach (string dir in Directory.GetDirectories(tk2dTexturesPath))
        {
            foreach (string file in Directory.GetFiles(dir))
            {
                if (Path.GetFileNameWithoutExtension(dir) != scName || Path.GetFileNameWithoutExtension(file) != atlasName)
                    continue;
                try
                {
                    texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(File.ReadAllBytes(file)))
                    {
                        Plugin.LogError($"Failed to load sprite texture {file}");
                        continue;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Plugin.LogError($"Exception while loading updated sprite textures: {e.Message}");
                    return false;
                }
            }
        }
        return false;
    }
}
