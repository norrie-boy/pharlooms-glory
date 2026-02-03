using BepInEx.Configuration;

namespace PharloomsGlory.Managers;

public class ConfigurationManager
{
    internal static ConfigurationManager instance = null;

    public static void Init()
    {
        instance ??= new ConfigurationManager();
    }

    // GENERAL
    //private ConfigEntry<bool> _mergeCoralMaps;
    //public bool MergeCoralMaps => _mergeCoralMaps.Value;

    // CORAL STEPS
    public enum AreaMusicTrack {
        VANILLA,
        CORAL_STEPS,
        RED_CORAL_GORGE
    }
    private ConfigEntry<AreaMusicTrack> _blastedStepsMusic;
    public AreaMusicTrack BlastedStepsMusic => _blastedStepsMusic.Value;
    //private ConfigEntry<bool> _removeSandcarvers;
    //public bool RemoveSandcarvers => _removeSandcarvers.Value;

    // RED CORAL GORGE
    private ConfigEntry<AreaMusicTrack> _sandsOfKarakMusic;
    public AreaMusicTrack SandsOfKarakMusic => _sandsOfKarakMusic.Value;

    public void Bind(ConfigFile config)
    {
        // GENERAL
        //_mergeCoralMaps = config.Bind("General", "MergeCoralMaps", false, "Merges Blasted Steps & Sands of Karak into one zone on the map");        // TODO

        // CORAL STEPS
        _blastedStepsMusic = config.Bind("Coral Steps", "BlastedStepsMusic", AreaMusicTrack.CORAL_STEPS, "Music track that plays in Blasted Steps (Restart game to apply)");
        //_removeSandcarvers = config.Bind("Coral Steps", "RemoveSandcarvers", false, "Replaces Sandcarvers in Blasted Steps with water");        // TODO

        // RED CORAL GORGE
        _sandsOfKarakMusic = config.Bind("Red Coral Gorge", "SandsOfKarakMusic", AreaMusicTrack.RED_CORAL_GORGE, "Music track that plays in Sands of Karak (Restart game to apply)");
    }
}
