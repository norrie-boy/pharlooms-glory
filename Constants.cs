using BepInEx;
using System.IO;
using UnityEngine;

namespace PharloomsGlory;

public class Constants
{
    public const string PLUGIN_GUID = "com.mars.pharloomsglory";
    public const string PLUGIN_NAME = "Pharloom's Glory";
    public const string PLUGIN_VERSION = "0.1.0";

    public static readonly string BASE_PLUGIN_PATH = $"\\\\?\\{Path.Combine(Paths.PluginPath, "PharloomsGlory")}";

    public static readonly string TEXTURES_PATH = Path.Combine(BASE_PLUGIN_PATH, "Textures");
    public static readonly string SPRITE_OFFSETS_FILE_NAME = "spriteOffsets.txt";
    public static readonly string TK2D_TEXTURES_DIRECTORY = "TK2D";
    public static readonly string AREA_ART_TEXTURES_DIRECTORY = "AREA_ART";
    public static readonly string HUD_TEXTURES_DIRECTORY = "HUD";

    public static readonly Color BLASTED_STEPS_MAP_COLOR = new Color(0.408f, 0.855f, 0.988f, 1f);

    public static readonly string SCENE_MODIFICATIONS_PATH = Path.Combine(BASE_PLUGIN_PATH, "Scenes");
    public static readonly string OBJECT_PATHS_FILE_PATH = Path.Combine(SCENE_MODIFICATIONS_PATH, "objectPaths.txt");
    public static readonly string OBJECT_RENAMES_FILE_PATH = Path.Combine(SCENE_MODIFICATIONS_PATH, "objectRenames.txt");
    public static readonly string RIVER_CLONES_DATA_FILE_PATH = Path.Combine(SCENE_MODIFICATIONS_PATH, "riverClones.txt");

    public static readonly string TEXT_MODIFICATIONS_PATH = Path.Combine(BASE_PLUGIN_PATH, "Text");

    public static readonly string AUDIO_PATH = Path.Combine(BASE_PLUGIN_PATH, "Audio");
}
