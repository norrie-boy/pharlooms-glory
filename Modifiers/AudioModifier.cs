using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PharloomsGlory.Modifiers;

public class AudioModifier
{
    internal static AudioModifier instance = null;

    public static void Init()
    {
        instance ??= new AudioModifier();
    }

    private readonly Dictionary<string, AudioClip> updatedClips = new Dictionary<string, AudioClip>();

    public void LoadAudioClips()
    {
        updatedClips.Clear();
        if (!Directory.Exists(Constants.AUDIO_PATH))
        {
            Plugin.LogError("Failed to find updated audio directory");
            return;
        }
        foreach (string file in Directory.GetFiles(Constants.AUDIO_PATH))
        {
            try
            {
                string url = $"file:///{Uri.EscapeUriString(file.Replace("\\\\?\\", "").Replace("\\", "/"))}";
                string clipName = Path.GetFileNameWithoutExtension(file);
                if (!GetAudioClip(url, clipName, out AudioClip clip))
                {
                    Plugin.LogError($"Failed to load audio clip {file}");
                    continue;
                }
                updatedClips.Add(clipName, clip);
            }
            catch (Exception e)
            {
                Plugin.LogError($"Exception while loading audio clip: {e.Message}");
            }
        }
        Plugin.LogInfo("Loaded audio clips");
    }

    private bool GetAudioClip(string url, string clipName, out AudioClip clip)
    {
        clip = null;
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone) { }
        if (request.result != UnityWebRequest.Result.Success)
            return false;
        clip = DownloadHandlerAudioClip.GetContent(request);
        clip.name = clipName;
        return true;
    }

    public bool GetAudioClip(string clipName, out AudioClip clip)
    {
        return updatedClips.TryGetValue(clipName, out clip);
    }
}
