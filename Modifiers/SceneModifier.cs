using PharloomsGlory.Components;
using PharloomsGlory.Managers;
using Silksong.AssetHelper.ManagedAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PharloomsGlory.Modifiers;

public class SceneModifier
{
    internal static SceneModifier instance = null;

    public static void Init()
    {
        instance ??= new SceneModifier();
    }

    public bool active = false;

    private readonly string[] CORAL_SCENES =
    [
        // Blasted Steps:
        "Coral_34",
        "Coral_11b",
        "Coral_11",
        "Coral_03",
        "Bellway_08",
        "Coral_35",
        "Coral_43",
        "Coral_33",
        "Coral_42",
        "Coral_36",
        "Coral_32",
        "Coral_Judge_Arena",
        "Coral_12",
        "Coral_37",
        "Coral_19",
        "Coral_19b",
        "Coral_02",
        "Room_Pinstress",
        // Sands of Karak:
        "Coral_35b",
        "Coral_23",
        "Coral_40",
        "Coral_24",
        "Coral_26",
        "Coral_38",
        "Bellshrine_Coral",
        "Coral_25",
        "Coral_44",
        "Coral_27",
        "Coral_28",
        "Coral_Tower_01",
        "Coral_41",
        "Coral_39"
    ];

    private readonly List<GameObject> currentlyAddedGameObjects = new List<GameObject>();

    private readonly List<string> parsedDeactivatedObjects = new List<string>();
    private struct DeactivatePointWithSpriteData
    {
        public Vector2 position;
        public float radius;
        public string spriteName;
        public float? depth;
    }
    private readonly List<DeactivatePointWithSpriteData> parsedDeactivatedPointsWithSprite = new List<DeactivatePointWithSpriteData>();
    public struct DeactivateWakeEffectData
    {
        public string range;
        public string effect;
    }
    private readonly List<DeactivateWakeEffectData> parsedDeactivatedWakeEffects = new List<DeactivateWakeEffectData>();
    private readonly List<string> parsedDeactivatedAudioObjects = new List<string>();
    private struct DeactivateDelayedObjectData
    {
        public string name;
        public float delay;
    }
    private readonly List<DeactivateDelayedObjectData> parsedDeactivateDelayedObjects = new List<DeactivateDelayedObjectData>();

    public struct FishParticleData
    {
        public float minCurveMin;
        public float minCurveMax;
        public float maxCurveMin;
        public float maxCurveMax;
        public float limit;
    }
    public struct CloneObjectData
    {
        public string name;
        public Vector3 position;
        public Vector3? rotation;
        public FishParticleData? fishParticleData;
        public float? surfaceWaterRegion;
        public Vector2? scale;
    }
    private readonly List<CloneObjectData> parsedClonedObjects = new List<CloneObjectData>();
    public struct AddObjectData
    {
        public string spriteName;
        public Vector3 position;
        public Vector3? rotation;
        public Vector2? scale;
        public Color? color;
    }
    private readonly List<AddObjectData> parsedAddedObjects = new List<AddObjectData>();
    private enum WaterfallLength
    {
        THIN,
        NORMAL,
        WIDE,
        HUGE
    }
    private enum WaterfallType
    {
        BG,
        FG
    }
    private struct WaterfallObjectData
    {
        public WaterfallLength length;
        public WaterfallType type;
        public bool noEffects;
        public Vector3 position;
        public Vector3? rotation;
        public Vector2? scale;
        public float? offsetY;
        public Color? color;
    }
    private readonly List<WaterfallObjectData> parsedWaterfallObjects = new List<WaterfallObjectData>();
    private struct WaterfallEffectsObjectData
    {
        public WaterfallLength length;
        public WaterfallType type;
        public Vector3 position;
        public Vector3? rotation;
        public Vector2? scale;
    }
    private readonly List<WaterfallEffectsObjectData> parsedWaterfallEffectObjects = new List<WaterfallEffectsObjectData>();
    private enum RiverOrientation
    {
        SLOPE,
        HORIZONTAL,
        VERTICAL,
        CORNER
    }
    private enum RiverType
    {
        BG,
        FG
    }
    private struct RiverObjectData
    {
        public RiverOrientation orientation;
        public RiverType type;
        public Vector3 position;
        public Vector3? rotation;
        public Vector2? scale;
    }
    private readonly List<RiverObjectData> parsedRiverObjects = new List<RiverObjectData>();
    private struct MoveObjectData
    {
        public string name;
        public Vector2 offset;
    }
    private readonly List<MoveObjectData> parsedMoveObjects = new List<MoveObjectData>();

    private bool modifyAllSprites;
    private readonly List<string> parsedSpriteIgnoreObjects = new List<string>();
    public struct SpriteModifyObjectData
    {
        public string name;
        public GlobalEnums.MapZone? mapZone;
    }
    private readonly List<SpriteModifyObjectData> parsedSpriteModifyObjects = new List<SpriteModifyObjectData>();
    public struct SpriteAlterPointData
    {
        public Vector2 position;
        public float radius;
        public GlobalEnums.MapZone? mapZone;
    }
    private readonly List<SpriteAlterPointData> parsedSpriteModifyPoints = new List<SpriteAlterPointData>();
    private readonly List<SpriteAlterPointData> parsedSpriteIgnorePoints = new List<SpriteAlterPointData>();
    private readonly List<SpriteModifyObjectData> parsedSpriteModifyObjectsWithMapZone = new List<SpriteModifyObjectData>();
    private readonly List<SpriteAlterPointData> parsedSpriteModifyPointsWithMapZone = new List<SpriteAlterPointData>();
    private GlobalEnums.MapZone? overrideMapZone;

    private Color? parsedHeroLightColor;
    private Color? parsedAmbientLightColor;
    private float? parsedAmbientLightIntensity;
    private float? parsedSaturation;

    private GlobalEnums.MapZone? parsedParticleType;
    private GlobalEnums.EnvironmentTypes? parsedEnvironmentType;

    private readonly Dictionary<string, Color> parsedTransitions = new Dictionary<string, Color>();

    public void SetActive(bool value)
    {
        if (active == value)
            return;
        else if (active && !value)
        {
            PatcherManager.instance.ResetColorValues();
            Util.Clear();
        }
        active = value;
        Plugin.LogInfo($"Set active SceneModifier to {value}");
    }

    public void Modify(Scene scene)
    {
        if (!CORAL_SCENES.Contains(scene.name))
        {
            SetActive(false);
            return;
        }
        if (!active)
            SetActive(true);

        if (!AssetManager.instance.IsLoaded)
            return;

        // ===== OBJECTS ====
        parsedDeactivatedObjects.Clear();
        parsedDeactivatedPointsWithSprite.Clear();
        parsedDeactivatedWakeEffects.Clear();
        parsedDeactivatedAudioObjects.Clear();
        parsedDeactivateDelayedObjects.Clear();
        parsedClonedObjects.Clear();
        parsedAddedObjects.Clear();
        parsedWaterfallObjects.Clear();
        parsedWaterfallEffectObjects.Clear();
        parsedRiverObjects.Clear();
        parsedMoveObjects.Clear();

        // ===== SPRITES ====
        modifyAllSprites = true;
        parsedSpriteIgnoreObjects.Clear();
        parsedSpriteModifyObjects.Clear();
        parsedSpriteModifyPoints.Clear();
        parsedSpriteIgnorePoints.Clear();
        parsedSpriteModifyObjectsWithMapZone.Clear();
        parsedSpriteModifyPointsWithMapZone.Clear();
        overrideMapZone = null;

        // ===== LIGHTING ====
        parsedHeroLightColor = null;
        parsedAmbientLightColor = null;
        parsedAmbientLightIntensity = null;
        parsedSaturation = null;

        // ===== PARTICLES =====
        parsedParticleType = null;
        parsedEnvironmentType = null;

        // ===== TRANSITION =====
        parsedTransitions.Clear();

        if (!Directory.Exists(Constants.SCENE_MODIFICATIONS_PATH))
        {
            Plugin.LogError("Failed to find scene modifications directory");
            return;
        }
        string modificationsPath = Path.Combine(Constants.SCENE_MODIFICATIONS_PATH, $"{scene.name}.txt");
        if (File.Exists(modificationsPath))
            ParseSceneModifications(File.ReadAllLines(modificationsPath));

        DeactivateGameObjects(scene);
        PatcherManager.instance.SetColorValues(parsedHeroLightColor, parsedAmbientLightColor, parsedAmbientLightIntensity, parsedSaturation);
        PatcherManager.instance.SetWakeEffects(parsedDeactivatedWakeEffects);
        AddGameObjects();
        MoveObjects(scene);
        ModifySpriteRenderers(scene);
        ModifySpriteCollections();
        ModifySceneManager();
        ModifyTransitions(scene);
        Plugin.LogInfo($"Modified {scene.name}");
    }

    private void ParseSceneModifications(string[] lines)
    {
        /*
            Scene modifications:
            ===== OBJECTS =====
            deactivate:<UniqueName>
            deactivatePointWithSprite:(<Vector2>);<float>@<SpriteName>
            deactivateWakeEffect:<UniqueName>@<UniqueName>
            deactivateAudio:<UniqueName>
            deactivateDelayed:<UniqueName>@<float>
            clone:<Path|Rename>@(<Vector3>)[rotation|scale|fish|surfaceWaterRegion]+
            add:<SpriteName>@(<Vector3>)[rotation|scale|color]+
            waterfall:<thin|normal|wide|huge>;<bg|fg>(;noEffects)?@(<Vector3>)[rotation|scale|offsetY|color]+
            waterfallEffects:<thin|normal|wide|huge>;<bg|fg>@(<Vector3>)[rotation|scale|color]+
            river:<slope|horizontal|vertical>;<bg|fg>@(<Vector3>)[rotation|scale|color]+
            move:<UniqueName>@(<Vector2>)
            ===== SPRITES =====
            spriteIgnoreObject:<UniqueName>
            spriteModifyObject:<UniqueName>
            spriteModifyPoint:(<Vector2>)@<float>
            spriteIgnorePoint:(<Vector2>)@<float>
            spriteModifyObjectWithMapZone:<UniqueName>@<int>
            spriteModifyPointWithMapZone:(<Vector2>);<float>@<int>
            overrideMapZone:<int>
            modifyAllSprites:<bool>
            ===== LIGHTING =====
            heroLightColor:(<R>,<G>,<B>,<A>)
            ambientLightColor:(<R>,<G>,<B>,<A>)
            ambientLightIntensity:<float>
            saturation:<float>
            ===== PARTICLES =====
            particles:<int>
            environment:<int>
            ===== TRANSITIONS =====
            transition:<UniqueName>@<Color>

            Arguments:
            rotation(<Vector3>)
            fish((<Min>,<Max>);(<Min>,<Max>);<float>)
            scale((<float>|<Vector2>))
            color(<Color>)
            offsetY(<float>)
        */
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            int index = line.IndexOf(":");
            if (index == -1)
            {
                Plugin.LogError($"Invalid scene modification syntax @ line {line}");
                continue;
            }
            string sceneModification = line[..index];
            string data = line[(index + 1)..];
            switch (sceneModification)
            {
                // ===== OBJECTS =====
                case "deactivate":
                    parsedDeactivatedObjects.Add(data);
                    break;
                case "deactivatePointWithSprite":
                    if (!ParseDeactivatePointWithSprite(data))
                        Plugin.LogError($"Failed to parse deactivatePointWithSprite scene modification @ line {line}");
                    break;
                case "deactivateWakeEffect":
                    if (!ParseDeactivateWakeEffect(data))
                        Plugin.LogError($"Failed to parse deactivateWakeEffect scene modification @ line {line}");
                    break;
                case "deactivateAudio":
                    parsedDeactivatedAudioObjects.Add(data);
                    break;
                case "deactivateDelayed":
                    if (!ParseDeactivateDelayed(data))
                        Plugin.LogError($"Failed to parse deactivateDelayed scene modification @ line {line}");
                    break;
                case "clone":
                    if (!ParseClone(data))
                        Plugin.LogError($"Failed to parse clone scene modification @ line {line}");
                    break;
                case "add":
                    if (!ParseAdd(data))
                        Plugin.LogError($"Failed to parse add scene modification @ line {line}");
                    break;
                case "waterfall":
                    if (!ParseWaterfall(data))
                        Plugin.LogError($"Failed to parse waterfall scene modification @ line {line}");
                    break;
                case "waterfallEffects":
                    if (!ParseWaterfallEffects(data))
                        Plugin.LogError($"Failed to parse waterfallEffects scene modification @ line {line}");
                    break;
                case "river":
                    if (!ParseRiver(data))
                        Plugin.LogError($"Failed to parse river scene modification @ line {line}");
                    break;
                case "move":
                    if (!ParseMove(data))
                        Plugin.LogError($"Failed to parse move scene modification @ line {line}");
                    break;
                // ===== SPRITES =====
                case "spriteIgnoreObject":
                    parsedSpriteIgnoreObjects.Add(data);
                    break;
                case "spriteModifyObject":
                    modifyAllSprites = false;
                    parsedSpriteModifyObjects.Add(new SpriteModifyObjectData()
                    {
                        name = data,
                        mapZone = null
                    });
                    break;
                case "spriteModifyPoint":
                    modifyAllSprites = false;
                    if (!ParseSpriteModifyPoint(data))
                        Plugin.LogError($"Failed to parse spriteModifyPoint scene modification @ line {line}");
                    break;
                case "spriteIgnorePoint":
                    if (!ParseSpriteIgnorePoint(data))
                        Plugin.LogError($"Failed to parse spriteIgnorePoint scene modification @ line {line}");
                    break;
                case "spriteModifyObjectWithMapZone":
                    modifyAllSprites = false;
                    if (!ParseSpriteModifyObjectWithMapZone(data))
                        Plugin.LogError($"Failed to parse spriteModifyObjectWithMapZone scene modification @ line {line}");
                    break;
                case "spriteModifyPointWithMapZone":
                    modifyAllSprites = false;
                    if (!ParseSpriteModifyPointWithMapZone(data))
                        Plugin.LogError($"Failed to parse spriteModifyPointWithMapZone scene modification @ line {line}");
                    break;
                case "overrideMapZone":
                    if (!ParseOverrideMapZone(data))
                        Plugin.LogError($"Failed to parse overrideMapZone scene modification @ line {line}");
                    break;
                case "modifyAllSprites":
                    if (!bool.TryParse(data, out modifyAllSprites))
                    {
                        Plugin.LogError($"Failed to parse modifyAllSprites scene modification @ line {line}");
                        modifyAllSprites = true;
                    }
                    break;
                // ===== LIGHTING =====
                case "heroLightColor":
                    if (!ParseHeroLightColor(data))
                        Plugin.LogError($"Failed to parse color for HeroLightColor @ line {line}");
                    break;
                case "ambientLightColor":
                    if (!ParseAmbientLightColor(data))
                        Plugin.LogError($"Failed to parse color for AmbientLightColor @ line {line}");
                    break;
                case "ambientLightIntensity":
                    if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float ambientLightIntensity))
                        parsedAmbientLightIntensity = ambientLightIntensity;
                    else
                        Plugin.LogError($"Failed to parse float value for AmbientLightIntensity @ line {line}");
                    break;
                case "saturation":
                    if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float saturation))
                        parsedSaturation = saturation;
                    else
                        Plugin.LogError($"Failed to parse float value for Saturation @ line {line}");
                    break;
                // ===== PARTICLES =====
                case "particles":
                    if (int.TryParse(data, out int mapZone))
                        parsedParticleType = (GlobalEnums.MapZone)mapZone;
                    else
                        Plugin.LogError($"Failed to parse int value for ParticleType @ line {line}");
                    break;
                case "environment":
                    if (int.TryParse(data, out int environmentType))
                        parsedEnvironmentType = (GlobalEnums.EnvironmentTypes)environmentType;
                    else
                        Plugin.LogError($"Failed to parse int value for EnvironmentType @ line {line}");
                    break;
                // ===== TRANSITIONS =====
                case "transition":
                    if (!ParseTransition(data))
                        Plugin.LogError($"Failed to parse transition scene modification @ line {line}");
                    break;
                default:
                    Plugin.LogError($"Invalid scene modification {sceneModification} @ line {line}");
                    continue;
            }
        }
    }

    private bool ParseDeactivatePointWithSprite(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string spriteName = splitData[1];

        string[] pointData = splitData[0].Split(";");
        if (pointData.Length != 2)
            return false;
        SceneModificationsParser parser = new SceneModificationsParser();
        bool hasDepth = parser.ParseVector3(pointData[0], out Vector3 positionWithDepth);
        if (!parser.ParseVector2(pointData[0], out Vector2 position) && !hasDepth || !float.TryParse(pointData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float radius))
            return false;

        DeactivatePointWithSpriteData parsedData = new DeactivatePointWithSpriteData()
        {
            position = hasDepth ? new Vector2(positionWithDepth.x, positionWithDepth.y) : position,
            radius = radius,
            spriteName = spriteName,
            depth = hasDepth ? positionWithDepth.z : null
        };
        parsedDeactivatedPointsWithSprite.Add(parsedData);
        return true;
    }

    private bool ParseDeactivateWakeEffect(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string uniqueName = splitData[0];
        string effectName = splitData[1];

        DeactivateWakeEffectData parsedData = new DeactivateWakeEffectData()
        {
            range = uniqueName,
            effect = effectName
        };
        parsedDeactivatedWakeEffects.Add(parsedData);
        return true;
    }

    private bool ParseDeactivateDelayed(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string uniqueName = splitData[0];
        if (!float.TryParse(splitData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float delay))
            return false;

        DeactivateDelayedObjectData parsedData = new DeactivateDelayedObjectData()
        {
            name = uniqueName,
            delay = delay
        };
        parsedDeactivateDelayedObjects.Add(parsedData);
        return true;
    }

    private bool ParseClone(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string name = splitData[0];

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector3(splitData[1], out Vector3 position))
            return false;
        bool hasRotation = parser.ParseArgument("rotation", splitData[1]);
        bool hasFishParticleData = parser.ParseArgument("fish", splitData[1]);
        bool hasSurfaceWaterRegion = parser.ParseArgument("surfaceWaterRegion", splitData[1]);
        bool hasScale = parser.ParseArgument("scale", splitData[1]);

        CloneObjectData parsedData = new CloneObjectData()
        {
            name = name,
            position = position,
            rotation = hasRotation ? parser.GetParsedRotation() : null,
            fishParticleData = hasFishParticleData ? parser.GetParsedFishParticleData() : null,
            surfaceWaterRegion = hasSurfaceWaterRegion ? parser.GetParsedSurfaceWaterRegion() : null,
            scale = hasScale ? parser.GetParsedScale() : null
        };
        parsedClonedObjects.Add(parsedData);
        return true;
    }

    private bool ParseAdd(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string spriteName = splitData[0];

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector3(splitData[1], out Vector3 position))
            return false;
        bool hasRotation = parser.ParseArgument("rotation", splitData[1]);
        bool hasScale = parser.ParseArgument("scale", splitData[1]);
        bool hasColor = parser.ParseArgument("color", splitData[1]);

        AddObjectData parsedData = new AddObjectData()
        {
            spriteName = spriteName,
            position = position,
            rotation = hasRotation ? parser.GetParsedRotation() : null,
            scale = hasScale ? parser.GetParsedScale() : null,
            color = hasColor ? parser.GetParsedColor() : null
        };
        parsedAddedObjects.Add(parsedData);
        return true;
    }

    private bool ParseWaterfall(string data)
    {
        string[] splitData = data.Split('@');
        if (splitData.Length != 2)
            return false;

        string[] objectData = splitData[0].Split(";");
        if (objectData.Length != 2 && objectData.Length != 3)
            return false;

        WaterfallLength length;
        switch (objectData[0])
        {
            case "thin":
                length = WaterfallLength.THIN;
                break;
            case "normal":
                length = WaterfallLength.NORMAL;
                break;
            case "wide":
                length = WaterfallLength.WIDE;
                break;
            case "huge":
                length = WaterfallLength.HUGE;
                break;
            default:
                return false;
        }
        WaterfallType type;
        switch (objectData[1])
        {
            case "bg":
                type = WaterfallType.BG;
                break;
            case "fg":
                type = WaterfallType.FG;
                break;
            default:
                return false;
        }
        bool noEffects = false;
        if (objectData.Length == 3)
        {
            if (objectData[2] != "noEffects")
                return false;
            noEffects = true;
        }

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector3(splitData[1], out Vector3 position))
            return false;
        bool hasRotation = parser.ParseArgument("rotation", splitData[1]);
        bool hasScale = parser.ParseArgument("scale", splitData[1]);
        bool hasOffsetY = parser.ParseArgument("offsetY", splitData[1]);
        bool hasColor = parser.ParseArgument("color", splitData[1]);

        WaterfallObjectData parsedData = new WaterfallObjectData()
        {
            length = length,
            type = type,
            noEffects = noEffects,
            position = position,
            rotation = hasRotation ? parser.GetParsedRotation() : null,
            scale = hasScale ? parser.GetParsedScale() : null,
            offsetY = hasOffsetY ? parser.GetParsedOffsetY() : null,
            color = hasColor ? parser.GetParsedColor() : null
        };
        parsedWaterfallObjects.Add(parsedData);
        return true;
    }

    private bool ParseWaterfallEffects(string data)
    {
        string[] splitData = data.Split('@');
        if (splitData.Length != 2)
            return false;

        string[] objectData = splitData[0].Split(";");
        if (objectData.Length != 2)
            return false;

        WaterfallLength length;
        switch (objectData[0])
        {
            case "thin":
                length = WaterfallLength.THIN;
                break;
            case "normal":
                length = WaterfallLength.NORMAL;
                break;
            case "wide":
                length = WaterfallLength.WIDE;
                break;
            case "huge":
                length = WaterfallLength.HUGE;
                break;
            default:
                return false;
        }
        WaterfallType type;
        switch (objectData[1])
        {
            case "bg":
                type = WaterfallType.BG;
                break;
            case "fg":
                type = WaterfallType.FG;
                break;
            default:
                return false;
        }

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector3(splitData[1], out Vector3 position))
            return false;
        bool hasRotation = parser.ParseArgument("rotation", splitData[1]);
        bool hasScale = parser.ParseArgument("scale", splitData[1]);

        WaterfallEffectsObjectData parsedData = new WaterfallEffectsObjectData()
        {
            length = length,
            type = type,
            position = position,
            rotation = hasRotation ? parser.GetParsedRotation() : null,
            scale = hasScale ? parser.GetParsedScale() : null
        };
        parsedWaterfallEffectObjects.Add(parsedData);
        return true;
    }

    private bool ParseRiver(string data)
    {
        string[] splitData = data.Split('@');
        if (splitData.Length != 2)
            return false;

        string[] objectData = splitData[0].Split(";");
        if (objectData.Length != 2)
            return false;

        RiverOrientation orientation;
        switch (objectData[0])
        {
            case "slope":
                orientation = RiverOrientation.SLOPE;
                break;
            case "horizontal":
                orientation = RiverOrientation.HORIZONTAL;
                break;
            case "vertical":
                orientation = RiverOrientation.VERTICAL;
                break;
            case "corner":
                orientation = RiverOrientation.CORNER;
                break;
            default:
                return false;
        }
        RiverType type;
        switch (objectData[1])
        {
            case "bg":
                type = RiverType.BG;
                break;
            case "fg":
                type = RiverType.FG;
                break;
            default:
                return false;
        }

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector3(splitData[1], out Vector3 position))
            return false;
        bool hasRotation = parser.ParseArgument("rotation", splitData[1]);
        bool hasScale = parser.ParseArgument("scale", splitData[1]);

        RiverObjectData parsedData = new RiverObjectData()
        {
            orientation = orientation,
            type = type,
            position = position,
            rotation = hasRotation ? parser.GetParsedRotation() : null,
            scale = hasScale ? parser.GetParsedScale() : null
        };
        parsedRiverObjects.Add(parsedData);
        return true;
    }

    private bool ParseMove(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string name = splitData[0];

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector2(splitData[1], out Vector2 offset))
            return false;

        MoveObjectData parsedData = new MoveObjectData()
        {
            name = name,
            offset = offset
        };
        parsedMoveObjects.Add(parsedData);
        return true;
    }

    private bool ParseSpriteModifyPoint(string data)
    {
        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseSpriteAlterPoint(data, out Vector2 position, out float radius))
            return false;

        SpriteAlterPointData parsedData = new SpriteAlterPointData()
        {
            position = position,
            radius = radius,
            mapZone = null
        };
        parsedSpriteModifyPoints.Add(parsedData);
        return true;
    }

    private bool ParseSpriteIgnorePoint(string data)
    {
        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseSpriteAlterPoint(data, out Vector2 position, out float radius))
            return false;

        SpriteAlterPointData parsedData = new SpriteAlterPointData()
        {
            position = position,
            radius = radius,
            mapZone = null
        };
        parsedSpriteIgnorePoints.Add(parsedData);
        return true;
    }

    private bool ParseSpriteModifyObjectWithMapZone(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string name = splitData[0];

        if (!int.TryParse(splitData[1], out int mapZone))
            return false;

        SpriteModifyObjectData parsedData = new SpriteModifyObjectData()
        {
            name = name,
            mapZone = (GlobalEnums.MapZone)mapZone
        };
        parsedSpriteModifyObjectsWithMapZone.Add(parsedData);
        return true;
    }

    private bool ParseSpriteModifyPointWithMapZone(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string[] pointData = splitData[0].Split(";");
        if (pointData.Length != 2)
            return false;
        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseVector2(pointData[0], out Vector2 position) || !float.TryParse(pointData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float radius) || !int.TryParse(splitData[1], out int mapZone))
            return false;

        SpriteAlterPointData parsedData = new SpriteAlterPointData()
        {
            position = position,
            radius = radius,
            mapZone = (GlobalEnums.MapZone)mapZone
        };
        parsedSpriteModifyPointsWithMapZone.Add(parsedData);
        return true;
    }

    private bool ParseOverrideMapZone(string data)
    {
        if (!int.TryParse(data, out int mapZone))
            return false;
        overrideMapZone = (GlobalEnums.MapZone)mapZone;
        return true;
    }

    private bool ParseHeroLightColor(string data)
    {
        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseColor(data, out Color heroLightColor))
            return false;
        parsedHeroLightColor = heroLightColor;
        return true;
    }

    private bool ParseAmbientLightColor(string data)
    {
        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseColor(data, out Color ambientLightColor))
            return false;
        parsedAmbientLightColor = ambientLightColor;
        return true;
    }

    private bool ParseTransition(string data)
    {
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        string objectName = splitData[0];

        SceneModificationsParser parser = new SceneModificationsParser();
        if (!parser.ParseColor(splitData[1], out Color color))
            return false;

        return parsedTransitions.TryAdd(objectName, color);
    }

    private void DeactivateGameObjects(Scene scene)
    {
        GameManager gm = GameManager.instance;
        GameManager.SceneTransitionFinishEvent deactivateObjects = null;
        deactivateObjects = delegate
        {
            DeactivateIndividualObjects(scene);
            DeactivatePoints(scene);
            DeactivateAudioObjects(scene);
            DeactivateDelayed(scene);
            gm.OnFinishedSceneTransition -= deactivateObjects;
        };
        gm.OnFinishedSceneTransition += deactivateObjects;
    }

    private void DeactivateIndividualObjects(Scene scene)
    {
        Dictionary<string, GameObject> gameObjects = Util.GetAllGameObjectsAsDictionary(scene);
        foreach (string name in parsedDeactivatedObjects)
        {
            if (gameObjects.TryGetValue(name, out GameObject go) && go != null)
                go.SetActive(false);
        }
    }

    private void DeactivatePoints(Scene scene)      // TODO: optimize
    {
        foreach (GameObject go in Util.GetAllGameObjects(scene))
        {
            if (go == null || !go.activeSelf)
                continue;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr?.sprite == null)
                continue;
            Vector3 position = go.transform.position;
            foreach (DeactivatePointWithSpriteData data in parsedDeactivatedPointsWithSprite)
            {
                if (Util.GetSpriteName(sr.sprite) == data.spriteName && Vector2.Distance(go.transform.position, data.position) <= data.radius)
                {
                    if (data.depth.HasValue && position.z < data.depth)
                        continue;
                    go.SetActive(false);
                }
            }
        }
    }

    private void DeactivateAudioObjects(Scene scene)
    {
        foreach (GameObject go in Util.GetAllGameObjects(scene))
        {
            if (go != null && parsedDeactivatedAudioObjects.Any(name => go.name.ToLower().Replace(" ", "_").Contains(name)))
                go.SetActive(false);
        }
    }

    private void DeactivateDelayed(Scene scene)
    {
        foreach (DeactivateDelayedObjectData data in parsedDeactivateDelayedObjects)
            GameManager.instance.StartCoroutine(DeactivateDelayedRoutine(scene, data.name, data.delay));
    }

    private IEnumerator DeactivateDelayedRoutine(Scene scene, string name, float delay)
    {
        yield return new WaitForSeconds(delay);
        Dictionary<string, GameObject> gameObjects = Util.GetAllGameObjectsAsDictionary(scene, true);
        if (!gameObjects.TryGetValue(name, out GameObject go))
            yield break;
        go.SetActive(false);
    }

    private void ModifySpriteRenderers(Scene scene)
    {
        GameManager gm = GameManager.instance;
        GameManager.SceneTransitionFinishEvent modifySpriteRenderers = null;
        modifySpriteRenderers = delegate
        {
            SpriteRendererModifier.instance.LoadSpriteRenderersWithIgnores(scene, parsedSpriteIgnoreObjects, parsedSpriteIgnorePoints);
            SpriteRendererModifier.instance.SetMapZone(overrideMapZone);
            if (modifyAllSprites)
            {
                foreach (SpriteAlterPointData data in parsedSpriteModifyPointsWithMapZone)
                    SpriteRendererModifier.instance.UpdatePoint(data);
                SpriteRendererModifier.instance.UpdateLoaded();
            }
            else
            {
                SpriteRendererModifier.instance.UpdateOnly(parsedSpriteModifyObjects.Concat(parsedSpriteModifyObjectsWithMapZone).ToList());
                foreach (SpriteAlterPointData data in parsedSpriteModifyPoints.Concat(parsedSpriteModifyPointsWithMapZone))
                    SpriteRendererModifier.instance.UpdatePoint(data);
            }
            gm.OnFinishedSceneTransition -= modifySpriteRenderers;
        };
        gm.OnFinishedSceneTransition += modifySpriteRenderers;
    }

    private void ModifySpriteCollections()
    {
        TK2DSpriteModifier.instance.LoadSpriteCollections();
        TK2DSpriteModifier.instance.UpdateLoaded();
    }

    private void AddGameObjects()
    {
        foreach (GameObject go in currentlyAddedGameObjects)
        {
            if (go == null)
                continue;
            GameObject.Destroy(go);
        }
        currentlyAddedGameObjects.Clear();
        GameManager gm = GameManager.instance;
        GameManager.SceneTransitionFinishEvent addObjects = null;
        addObjects = delegate
        {
            AddClonedObjects();
            AddNewObjects();
            AddWaterfallObjects();
            AddWaterfallEffectsObjects();
            AddRiverObjects();
            gm.OnFinishedSceneTransition -= addObjects;
        };
        gm.OnFinishedSceneTransition += addObjects;
    }

    private void AddClonedObjects()
    {
        foreach (CloneObjectData data in parsedClonedObjects)
        {
            if (!AssetManager.instance.GetAsset(data.name, out ManagedAsset<GameObject> asset))
            {
                Plugin.LogError($"Failed to get requested asset {data.name}");
                continue;
            }

            GameObject clone = asset.InstantiateAsset();
            clone.transform.position = data.position;
            clone.transform.rotation = data.rotation.HasValue ? Quaternion.Euler(data.rotation.Value) : Quaternion.identity;
            clone.SetActive(true);
            if (data.scale.HasValue)
                ScaleObject(clone, data.scale.Value);
            currentlyAddedGameObjects.Add(clone);

            if (data.fishParticleData.HasValue)
                HandleFishParticle(clone, data.fishParticleData.Value, asset.InstantiateAsset());
            if (data.surfaceWaterRegion.HasValue)
                HandleSurfaceWaterRegion(clone, data.surfaceWaterRegion.Value);
        }
    }

    private void HandleFishParticle(GameObject go, FishParticleData data, GameObject original)
    {
        GameObject manager = new GameObject($"{Util.GetGameObjectUniqueName(go)} manager");
        manager.transform.position = go.transform.position;
        manager.transform.rotation = go.transform.rotation;
        go.transform.parent = manager.transform;
        FishParticleManager fpl = manager.AddComponent<FishParticleManager>();
        fpl.original = original;
        fpl.original.SetActive(false);
        fpl.data = data;
        fpl.current = go;
        fpl.StartReloadLoop();
        currentlyAddedGameObjects.Remove(go);
        currentlyAddedGameObjects.Add(manager);
    }

    private void HandleSurfaceWaterRegion(GameObject go, float flowSpeed)
    {
        SurfaceWaterRegion swr = go.GetComponent<SurfaceWaterRegion>();
        FieldInfo flowSpeedFieldInfo = typeof(SurfaceWaterRegion).GetField("flowSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        if (flowSpeedFieldInfo == null)
        {
            Plugin.LogError("Failed to get flowSpeed field info");
            return;
        }
        flowSpeedFieldInfo.SetValue(swr, flowSpeed);
    }

    private void AddNewObjects()
    {
        foreach (AddObjectData data in parsedAddedObjects)
        {
            if (!SpriteRendererModifier.instance.GetTexture(data.spriteName, out Texture2D t))
            {
                Plugin.LogError($"Failed to add object, the sprite {data.spriteName} is not loaded");
                continue;
            }
            GameObject go = new GameObject(data.spriteName);
            go.transform.position = data.position;
            go.transform.rotation = data.rotation.HasValue ? Quaternion.Euler(data.rotation.Value) : Quaternion.identity;
            if (data.scale.HasValue)
                ScaleObject(go, data.scale.Value);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(
                t,
                new Rect(0, 0, t.width, t.height),
                new Vector2(0.5f, 0.5f)
            );
            if (data.color.HasValue)
                sr.color = data.color.Value;
            currentlyAddedGameObjects.Add(go);
        }
    }

    private void AddWaterfallObjects()
    {
        foreach (WaterfallObjectData data in parsedWaterfallObjects)
        {
            string baseObjectPath;
            string splashObjectPath =
                data.type == WaterfallType.BG
                ? "waterfall_base_large (1)/particle_barrel_splash (4)"
                : "waterfall_base_large (1)/particle_barrel_splash (5)";
            Color tint = data.type == WaterfallType.BG ? Color.white : Color.black;
            if (data.color.HasValue)
                tint = data.color.Value;

            WaterfallModifiers modifiers = GetWaterfallModifiers(data.length);
            baseObjectPath = modifiers.baseObjectPath;
            Vector2 baseScale = new Vector2(modifiers.baseScale, 1f);
            float steamScale = modifiers.steamScale;
            float splashScale = modifiers.splashScale;

            if (!AssetManager.instance.GetAsset(baseObjectPath, out ManagedAsset<GameObject> baseObjectAsset))
            {
                Plugin.LogError($"Failed to get base object asset {baseObjectPath} for waterfall");
                continue;
            }

            GameObject waterfall = new GameObject($"waterfall-{data.length}-{data.type}-x{(int)data.position.x}y{(int)data.position.y}");
            waterfall.transform.position = data.position;
            
            Quaternion rotation = data.rotation.HasValue ? Quaternion.Euler(data.rotation.Value) : Quaternion.identity;
            baseScale *= data.scale ?? Vector2.one;
            steamScale *= data.scale.HasValue ? data.scale.Value.x : 1f;
            splashScale *= data.scale.HasValue ? data.scale.Value.x : 1f;
            if (data.type == WaterfallType.FG)
                splashScale *= 3.6f;

            GameObject baseClone = baseObjectAsset.InstantiateAsset();
            baseClone.transform.position = data.position;
            baseClone.transform.rotation = rotation;
            baseClone.SetActive(true);
            baseClone.transform.parent = waterfall.transform;
            MeshRenderer mr = baseClone.GetComponent<MeshRenderer>();
            foreach (Material m in mr.materials)
            {
                int tintColorProperty = Shader.PropertyToID("_TintColor");
                if (!m.HasProperty(tintColorProperty))
                    continue;
                int tintColorProp = tintColorProperty;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                mr.GetPropertyBlock(block);
                block.SetColor(tintColorProp, tint);
                mr.SetPropertyBlock(block);
            }
            ScaleObject(baseClone, baseScale);
            if (data.offsetY.HasValue)
                baseClone.transform.SetPositionY(baseClone.transform.GetPositionY() + data.offsetY.Value);

            if (data.noEffects)
            {
                currentlyAddedGameObjects.Add(waterfall);
                continue;
            }

            if (data.type != WaterfallType.FG)
            {
                if (!AssetManager.instance.GetAsset("waterfall_base_large (1)/Fungus_Steam (2)", out ManagedAsset<GameObject> steamAsset))
                {
                    Plugin.LogError("Failed to get steam asset waterfall_base_large (1)/Fungus_Steam (2) for waterfall");
                    continue;
                }

                GameObject steam = steamAsset.InstantiateAsset();
                steam.transform.position = data.position;
                steam.transform.rotation = rotation;
                steam.SetActive(true);
                steam.transform.parent = waterfall.transform;
                steam.transform.SetPositionZ(steam.transform.GetPositionZ() - 1f);
                ScaleObject(steam, steamScale);
            }

            if (!AssetManager.instance.GetAsset(splashObjectPath, out ManagedAsset<GameObject> splashAsset))
            {
                Plugin.LogError($"Failed to get splash asset {splashObjectPath} for waterfall");
                continue;
            }
            GameObject splash = splashAsset.InstantiateAsset();
            splash.transform.position = data.position;
            splash.transform.rotation = rotation;
            splash.SetActive(true);
            splash.transform.parent = waterfall.transform;
            splash.transform.SetPositionZ(splash.transform.position.z + 0.05f);
            ScaleObject(splash, splashScale);

            if (!AssetManager.instance.GetAsset("Group (6)/Audio Waterfall Small", out ManagedAsset<GameObject> audioAsset))
            {
                Plugin.LogError("Failed to get audio asset Group (6)/Audio Waterfall Small for waterfall");
                continue;
            }
            GameObject audio = audioAsset.InstantiateAsset();
            audio.transform.position = data.position;
            audio.transform.rotation = Quaternion.identity;
            audio.SetActive(true);
            audio.transform.parent = waterfall.transform;

            currentlyAddedGameObjects.Add(waterfall);
        }
    }

    private struct WaterfallModifiers
    {
        public string baseObjectPath;
        public float baseScale;
        public float steamScale;
        public float splashScale;
    }
    private WaterfallModifiers GetWaterfallModifiers(WaterfallLength length)
    {
        return length switch
        {
            WaterfallLength.THIN => new WaterfallModifiers()
            {
                baseObjectPath = AssetManager.instance.GetPathFromRename("waterfallNormal"),
                baseScale = 0.2f,
                steamScale = 0.03f,
                splashScale = 0.06f
            },
            WaterfallLength.NORMAL => new WaterfallModifiers()
            {
                baseObjectPath = AssetManager.instance.GetPathFromRename("waterfallNormal"),
                baseScale = 0.7f,
                steamScale = 0.2f,
                splashScale = 0.15f
            },
            WaterfallLength.WIDE => new WaterfallModifiers()
            {
                baseObjectPath = AssetManager.instance.GetPathFromRename("waterfallWide"),
                baseScale = 1f,
                steamScale = 0.2f,
                splashScale = 0.3f
            },
            WaterfallLength.HUGE => new WaterfallModifiers()
            {
                baseObjectPath = AssetManager.instance.GetPathFromRename("waterfallHuge"),
                baseScale = 1f,
                steamScale = 0.6f,
                splashScale = 0.6f
            },
            _ => new WaterfallModifiers(),
        };
    }

    private void ScaleObject(GameObject go, Vector2 scale)
    {
        go.transform.SetScaleX(go.transform.GetScaleX() * scale.x);
        go.transform.SetScaleY(go.transform.GetScaleY() * scale.y);
    }

    private void ScaleObject(GameObject go, float scale)
    {
        go.transform.SetScaleX(go.transform.GetScaleX() * scale);
        go.transform.SetScaleY(go.transform.GetScaleY() * scale);
    }

    private void AddWaterfallEffectsObjects()
    {
        foreach (WaterfallEffectsObjectData data in parsedWaterfallEffectObjects)
        {
            string splashObjectPath =
                data.type == WaterfallType.BG
                ? "waterfall_base_large (1)/particle_barrel_splash (4)"
                : "waterfall_base_large (1)/particle_barrel_splash (5)";
            WaterfallModifiers modifiers = GetWaterfallModifiers(data.length);
            float steamScale = modifiers.steamScale;
            float splashScale = modifiers.splashScale;

            GameObject waterfallEffects = new GameObject($"waterfallEffects-{data.length}-{data.type}-x{(int)data.position.x}y{(int)data.position.y}");
            waterfallEffects.transform.position = data.position;

            Quaternion rotation = data.rotation.HasValue ? Quaternion.Euler(data.rotation.Value) : Quaternion.identity;
            steamScale *= data.scale.HasValue ? data.scale.Value.x : 1f;
            splashScale *= data.scale.HasValue ? data.scale.Value.x : 1f;
            if (data.type == WaterfallType.FG)
                splashScale *= 3.6f;

            if (data.type != WaterfallType.FG)
            {
                if (!AssetManager.instance.GetAsset("waterfall_base_large (1)/Fungus_Steam (2)", out ManagedAsset<GameObject> steamAsset))
                {
                    Plugin.LogError("Failed to get steam asset waterfall_base_large (1)/Fungus_Steam (2) for waterfall effects");
                    continue;
                }

                GameObject steam = steamAsset.InstantiateAsset();
                steam.transform.position = data.position;
                steam.transform.rotation = rotation;
                steam.SetActive(true);
                steam.transform.parent = waterfallEffects.transform;
                steam.transform.SetPositionZ(steam.transform.GetPositionZ() - 1f);
                ScaleObject(steam, steamScale);
            }

            if (!AssetManager.instance.GetAsset(splashObjectPath, out ManagedAsset<GameObject> splashAsset))
            {
                Plugin.LogError($"Failed to get splash asset {splashObjectPath} for waterfall effects");
                continue;
            }
            GameObject splash = splashAsset.InstantiateAsset();
            splash.transform.position = data.position;
            splash.transform.rotation = rotation;
            splash.SetActive(true);
            splash.transform.parent = waterfallEffects.transform;
            splash.transform.SetPositionZ(splash.transform.position.z + 0.05f);
            ScaleObject(splash, splashScale);

            if (!AssetManager.instance.GetAsset("Group (6)/Audio Waterfall Small", out ManagedAsset<GameObject> audioAsset))
            {
                Plugin.LogError("Failed to get audio asset Group (6)/Audio Waterfall Small for waterfall effects");
                continue;
            }
            GameObject audio = audioAsset.InstantiateAsset();
            audio.transform.position = data.position;
            audio.transform.rotation = Quaternion.identity;
            audio.SetActive(true);
            audio.transform.parent = waterfallEffects.transform;

            currentlyAddedGameObjects.Add(waterfallEffects);
        }
    }

    private void AddRiverObjects()
    {
        foreach (RiverObjectData data in parsedRiverObjects)
        {
            Quaternion rotation = data.rotation.HasValue ? Quaternion.Euler(data.rotation.Value) : Quaternion.identity;
            switch (data.orientation)
            {
                case RiverOrientation.SLOPE:
                    {
                        string textureColor = data.type == RiverType.BG ? "blue" : "black";
                        string backTextureName = $"coral_river_tiled_0001_side_back-{textureColor}";
                        string frontTextureName = $"coral_river_tiled_0000_side_front-{textureColor}";

                        if (!AssetManager.instance.GetAsset("coral_river_chunk/river_top", out ManagedAsset<GameObject> riverAsset))
                        {
                            Plugin.LogError("Failed to get river asset coral_river_chunk/river_top for slope river");
                            continue;
                        }

                        GameObject riverSlope = riverAsset.InstantiateAsset();
                        riverSlope.transform.position = data.position;
                        riverSlope.transform.rotation = rotation;
                        riverSlope.SetActive(true);
                        if (data.scale.HasValue)
                            ScaleObject(riverSlope, data.scale.Value);
                        currentlyAddedGameObjects.Add(riverSlope);

                        HandleRiverClone(riverSlope, "river_top/Base_End", new AssetManager.RiverCloneData() {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = backTextureName
                        });
                        HandleRiverClone(riverSlope, "river_top/Base_End/Top", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = frontTextureName
                        });
                        HandleRiverClone(riverSlope, "river_top/Base", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = backTextureName
                        });
                        HandleRiverClone(riverSlope, "river_top/Base/Top", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = frontTextureName
                        });
                    }
                    break;
                case RiverOrientation.HORIZONTAL:
                    {
                        if (!AssetManager.instance.GetAsset("water_components_short_simple_white/StillWater", out ManagedAsset<GameObject> riverAsset))
                        {
                            Plugin.LogError("Failed to get river asset water_components_short_simple_white/StillWater for horizontal river");
                            continue;
                        }

                        GameObject riverHorizontal = riverAsset.InstantiateAsset();
                        riverHorizontal.transform.position = data.position;
                        riverHorizontal.transform.rotation = rotation;
                        riverHorizontal.SetActive(true);
                        if (data.scale.HasValue)
                            ScaleObject(riverHorizontal, data.scale.Value);
                        currentlyAddedGameObjects.Add(riverHorizontal);

                        HandleRiverClone(riverHorizontal, "StillWater", new AssetManager.RiverCloneData
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = "gradientBlue"
                        });

                        if (!AssetManager.instance.GetAsset("water_components_short_simple_white/waterways_water_components/acid_water_top", out ManagedAsset<GameObject> glowAsset))
                        {
                            Plugin.LogError("Failed to get glow asset water_components_short_simple_white/waterways_water_components/acid_water_top for horizontal river");
                            continue;
                        }

                        GameObject riverHorizontalGlow = glowAsset.InstantiateAsset();
                        riverHorizontalGlow.transform.position = data.position;
                        riverHorizontalGlow.transform.rotation = rotation;
                        riverHorizontalGlow.SetActive(true);
                        riverHorizontalGlow.transform.SetPositionX(riverHorizontalGlow.transform.GetPositionX() + 46f * (data.scale?.x ?? 1f));
                        riverHorizontalGlow.transform.SetPositionY(riverHorizontalGlow.transform.GetPositionY() + 2.75f * (data.scale?.y ?? 1f));
                        ScaleObject(riverHorizontalGlow, new Vector2(25f, 2f) * (data.scale ?? Vector2.one));
                        currentlyAddedGameObjects.Add(riverHorizontalGlow);

                        riverHorizontalGlow.AddComponent<RiverHorizontalGlowManager>();
                    }
                    break;
                case RiverOrientation.VERTICAL:
                    {
                        string textureColor = data.type == RiverType.BG ? "blue" : "black";
                        string backTextureName = $"coral_river_tiled_0003_straight_back-{textureColor}";
                        string frontTextureName = $"coral_river_tiled_0002_straight_front-{textureColor}";

                        if (!AssetManager.instance.GetAsset("coral_river_chunk/waterfall", out ManagedAsset<GameObject> riverAsset))
                        {
                            Plugin.LogError("Failed to get river asset coral_river_chunk/waterfall for vertical river");
                            continue;
                        }

                        GameObject riverVertical = riverAsset.InstantiateAsset();
                        riverVertical.transform.position = data.position;
                        riverVertical.transform.rotation = rotation;
                        riverVertical.SetActive(true);
                        if (data.scale.HasValue)
                            ScaleObject(riverVertical, data.scale.Value);
                        currentlyAddedGameObjects.Add(riverVertical);

                        HandleRiverClone(riverVertical, "waterfall/Base_End", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = backTextureName
                        });
                        HandleRiverClone(riverVertical, "waterfall/Base_End/Top", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = frontTextureName
                        });
                        HandleRiverClone(riverVertical, "waterfall/Base", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = backTextureName
                        });
                        HandleRiverClone(riverVertical, "waterfall/Base/Top", new AssetManager.RiverCloneData()
                        {
                            component = AssetManager.RiverCloneData.ComponentType.MESH_RENDERER,
                            resourceType = AssetManager.RiverCloneData.ResourceType.TEXTURE,
                            resourcePath = frontTextureName
                        });
                    }
                    break;
                case RiverOrientation.CORNER:
                    {
                        string textureColor = data.type == RiverType.BG ? "blue" : "black";

                        if (!AssetManager.instance.GetAsset("coral_river_chunk/corner", out ManagedAsset<GameObject> riverAsset))
                        {
                            Plugin.LogError("Failed to get river asset coral_river_chunk/corner for river corner");
                            continue;
                        }

                        GameObject riverCorner = riverAsset.InstantiateAsset();
                        riverCorner.transform.position = data.position;
                        riverCorner.transform.rotation = rotation;
                        riverCorner.SetActive(true);
                        if (data.scale.HasValue)
                            ScaleObject(riverCorner, data.scale.Value);
                        currentlyAddedGameObjects.Add(riverCorner);

                        RiverCornerManager rcm = riverCorner.AddComponent<RiverCornerManager>();
                        rcm.spriteSuffix = textureColor;
                    }
                    break;
            }
        }
    }

    private void HandleRiverClone(GameObject go, string? overrideChildPath = null, AssetManager.RiverCloneData? overrideData = null)
    {
        if (overrideChildPath != null && overrideData.HasValue)
        {
            ApplyRiverCloneData(go, overrideChildPath, overrideData.Value);
            return;
        }

        foreach (Dictionary<string, List<AssetManager.RiverCloneData>> d in AssetManager.instance.GetRiverClonesData())
        {
            foreach (KeyValuePair<string, List<AssetManager.RiverCloneData>> kvp in d)
            {
                if (kvp.Key.Split("/")[0] == go.name.Replace("(Clone)", ""))
                {
                    foreach (AssetManager.RiverCloneData data in kvp.Value)
                        ApplyRiverCloneData(go, kvp.Key, data);
                }
            }
        }
    }

    private void ApplyRiverCloneData(GameObject go, string childPath, AssetManager.RiverCloneData data)
    {
        string name = go.name.Replace("(Clone)", "");
        Texture2D? texture = null;
        Shader? shader = null;
        switch (data.resourceType)
        {
            case AssetManager.RiverCloneData.ResourceType.TEXTURE:
                if (!SpriteRendererModifier.instance.GetTexture(data.resourcePath, out texture, GlobalEnums.MapZone.CORAL_CAVERNS))
                {
                    Plugin.LogError($"Failed to get texture {data.resourcePath} in river clone data of {name}");
                    return;
                }
                break;
            case AssetManager.RiverCloneData.ResourceType.SHADER:
                shader = Shader.Find(data.resourcePath);
                if (shader == null)
                {
                    Plugin.LogError($"Failed to get shader {data.resourcePath} in river clone data of {name}");
                    return;
                }
                break;
            default:
                Plugin.LogError($"Invalid resource type in river clone data of {name}");
                return;
        }
        switch (data.component)
        {
            case AssetManager.RiverCloneData.ComponentType.MESH_RENDERER:
                {
                    if (!ParseRiverCloneDataPath(childPath, go, out GameObject child))
                    {
                        Plugin.LogError($"Failed to parse path {childPath} in river clone data of {name}");
                        return;
                    }
                    MeshRenderer mr = child.GetComponent<MeshRenderer>();
                    if (texture != null)
                    {
                        foreach (Material m in mr.materials)
                            m.color = new Color(1f, 1f, 1f, 0f);
                        mr.material.mainTexture = texture;
                        mr.material.color = new Color(1f, 1f, 1f, 1f);
                    }
                    if (shader != null)
                        mr.material.shader = shader;
                }
                break;
            default:
                Plugin.LogError($"Invalid component type in river clone data of {name}");
                return;
        }
    }

    private bool ParseRiverCloneDataPath(string path, GameObject go, out GameObject child)
    {
        child = go;
        string[] splitPath = path.Split('/');
        if (splitPath[0] != go.name.Replace("(Clone)", ""))
            return false;

        for (int i = 1; i < splitPath.Length; i++)
        {
            foreach (Transform t in child.GetComponentsInChildren<Transform>())
            {
                if (t.gameObject.name == splitPath[i] && t.parent == child.transform)
                {
                    child = t.gameObject;
                    continue;
                }
            }
        }
        return true;
    }

    private void MoveObjects(Scene scene)
    {
        GameManager gm = GameManager.instance;
        GameManager.SceneTransitionFinishEvent moveObjects = null;
        moveObjects = delegate
        {
            Dictionary<string, GameObject> gameObjects = Util.GetAllGameObjectsAsDictionary(scene);
            foreach (MoveObjectData data in parsedMoveObjects)
            {
                if (gameObjects.TryGetValue(data.name, out GameObject go))
                {
                    go.transform.SetPositionX(go.transform.GetPositionX() + data.offset.x);
                    go.transform.SetPositionY(go.transform.GetPositionY() + data.offset.y);
                }
            }
            gm.OnFinishedSceneTransition -= moveObjects;
        };
        gm.OnFinishedSceneTransition += moveObjects;
        
    }

    private void ModifySceneManager()
    {
        GameManager gm = GameManager.instance;
        GameManager.SceneTransitionFinishEvent modifySM = null;
        modifySM = delegate
        {
            CustomSceneManager sm = gm.sm;
            if (sm == null)
                return;
            if (parsedParticleType.HasValue)
                sm.overrideParticlesWith = parsedParticleType.Value;
            if (parsedEnvironmentType.HasValue)
                sm.environmentType = parsedEnvironmentType.Value;
            if (sm.isWindy)
                sm.isWindy = false;
            gm.OnBeforeFinishedSceneTransition -= modifySM;
        };
        gm.OnBeforeFinishedSceneTransition += modifySM;
    }

    private void ModifyTransitions(Scene scene)
    {
        Dictionary<string, GameObject> gameObjects = Util.GetAllGameObjectsAsDictionary(scene);
        foreach (KeyValuePair<string, Color> kvp in parsedTransitions)
        {
            if (gameObjects.TryGetValue(kvp.Key, out GameObject go))
            {
                SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
                sr?.color = kvp.Value;
            }
        }
    }
}
