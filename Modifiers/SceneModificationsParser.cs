using System;
using UnityEngine;

namespace PharloomsGlory.Modifiers;

public class SceneModificationsParser
{
    private Vector3 parsedRotation;
    private Vector2 parsedScale;
    private SceneModifier.FishParticleData parsedFishParticleData;
    private float parsedSurfaceWaterRegion;
    private Color parsedColor;
    private float parsedOffsetY;

    public SceneModificationsParser()
    {
        parsedSurfaceWaterRegion = 0;
        parsedOffsetY = 0;
    }

    public bool ParseVector2(string data, out Vector2 result)
    {
        result = Vector2.zero;
        Tuple<bool, int, int> indices = VerifyParentheses(data);
        if (!indices.Item1)
            return false;

        string[] values = data[indices.Item2..indices.Item3].Replace("(", "").Replace(")", "").Split(",");
        if (values.Length != 2)
            return false;
        values[0] = values[0];
        values[1] = values[1];
        if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
        {
            result = new Vector2(x, y);
            return true;
        }
        return false;
    }

    public bool ParseVector3(string data, out Vector3 result)
    {
        result = Vector3.zero;
        Tuple<bool, int, int> indices = VerifyParentheses(data);
        if (!indices.Item1)
            return false;

        string[] values = data[indices.Item2..indices.Item3].Replace("(", "").Replace(")", "").Split(",");
        if (values.Length != 3)
            return false;
        values[0] = values[0];
        values[2] = values[2];
        if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y) && float.TryParse(values[2], out float z))
        {
            result = new Vector3(x, y, z);
            return true;
        }
        return false;
    }

    public bool ParseColor(string data, out Color result)
    {
        result = Color.white;
        Tuple<bool, int, int> indices = VerifyParentheses(data);
        if (!indices.Item1)
            return false;

        string[] values = data[indices.Item2..indices.Item3].Replace("(", "").Replace(")", "").Split(",");
        if (values.Length != 4)
            return false;
        values[0] = values[0];
        values[3] = values[3];
        if (float.TryParse(values[0], out float r) && float.TryParse(values[1], out float g) && float.TryParse(values[2], out float b) && float.TryParse(values[3], out float a))
        {
            result = new Color(r, g, b, a);
            return true;
        }
        return false;
    }

    public bool ParseSpriteAlterPoint(string data, out Vector2 position, out float radius)
    {
        position = Vector2.zero;
        radius = 0;
        string[] splitData = data.Split("@");
        if (splitData.Length != 2)
            return false;

        return ParseVector2(splitData[0], out position) && float.TryParse(splitData[1], out radius);
    }

    public bool ParseArgument(string argument, string fullData)
    {
        int argumentIndex = fullData.IndexOf(argument);
        if (argumentIndex == -1)
            return false;
        Tuple<bool, int, int> indices = VerifyParentheses(fullData[argumentIndex..]);
        if (!indices.Item1)
            return false;

        string argumentData = fullData[(argumentIndex + indices.Item2)..(argumentIndex + indices.Item3)];

        switch (argument)
        {
            case "rotation":
                return ParseRotation(argumentData);
            case "scale":
                return ParseScale(argumentData);
            case "fish":
                return ParseFishParticleData(argumentData);
            case "surfaceWaterRegion":
                return ParseSurfaceWaterRegion(argumentData);
            case "color":
                return ParseColor(argumentData);
            case "offsetY":
                return ParseOffsetY(argumentData);
            default:
                Plugin.LogError($"Invalid scene modification argument {argument}");
                return false;
        }
    }

    private Tuple<bool, int, int> VerifyParentheses(string data)
    {
        int startIndex = data.IndexOf("(");
        if (startIndex == -1)
            return new Tuple<bool, int, int>(false, -1, -1);
        int endIndex = startIndex;
        int aux = 0;
        while (endIndex < data.Length + 1)
        {
            if(data[endIndex] == '(')
                aux++;
            else if (data[endIndex] == ')')
                aux--;
            endIndex++;
            if (aux == 0)
                break;
        }
        return aux == 0 ? new Tuple<bool, int, int>(true, startIndex, endIndex) : new Tuple<bool, int, int>(false, -1, -1);
    }

    private bool ParseRotation(string data)
    {
        return ParseVector3(data, out parsedRotation);
    }

    private bool ParseScale(string data)
    {
        if (ParseVector2(data, out parsedScale))
            return true;
        else if (float.TryParse(data.Replace("(", "").Replace(")", ""), out float scale))
        {
            parsedScale.x = scale;
            parsedScale.y = scale;
            return true;
        }
        return false;
    }

    private bool ParseColor(string data)
    {
        return ParseColor(data, out parsedColor);
    }

    private bool ParseFishParticleData(string data)
    {
        parsedFishParticleData = new SceneModifier.FishParticleData();
        string[] splitData = data[1..(data.Length - 1)].Split(";");
        if (splitData.Length != 3)
            return false;
        if (ParseVector2(splitData[0], out Vector2 minRange) && ParseVector2(splitData[1], out Vector2 maxRange) && float.TryParse(splitData[2], out float limit))
        {
            parsedFishParticleData = new SceneModifier.FishParticleData()
            {
                minCurveMin = minRange.x,
                minCurveMax = minRange.y,
                maxCurveMax = maxRange.x,
                maxCurveMin = maxRange.y,
                limit = limit
            };
            return true;
        }
        return false;
    }

    private bool ParseSurfaceWaterRegion(string data)
    {
        return float.TryParse(data.Replace("(", "").Replace(")", ""), out parsedSurfaceWaterRegion);
    }

    private bool ParseOffsetY(string data)
    {
        return float.TryParse(data.Replace("(", "").Replace(")", ""), out parsedOffsetY);
    }

    public Vector3 GetParsedRotation()
    {
        return parsedRotation;
    }

    public Vector2 GetParsedScale()
    {
        return parsedScale;
    }

    public SceneModifier.FishParticleData GetParsedFishParticleData()
    {
        return parsedFishParticleData;
    }

    public float GetParsedSurfaceWaterRegion()
    {
        return parsedSurfaceWaterRegion;
    }

    public Color GetParsedColor()
    {
        return parsedColor;
    }

    public float GetParsedOffsetY()
    {
        return parsedOffsetY;
    }
}
