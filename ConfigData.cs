using Dayz_Fishing_Bot;
using System.Text.Json;

class ConfigData
{
    public string TemplatesFolder { get; set; } = "templates"; // Fish templates
    public string GeneralFolder { get; set; } = "general"; // Basic search templates
    public double MatchThreshold { get; set; } = 0.82; // Percentage of matches by template. Higher is stricter. 

    // Randomized left-click hold duration (in milliseconds)
    public int HoldLeftMinMs { get; set; } = 25000;
    public int HoldLeftMaxMs { get; set; } = 30000;

    // Randomized duration after holding (in milliseconds)
    public int DelayAfterHoldMinMs { get; set; } = 250;
    public int DelayAfterHoldMaxMs { get; set; } = 600;

    // Amplitude of random cursor jitter in pixels
    public int JitterAmpPx { get; set; } = 6;

    // Randomized time for dragging the cursor to an object
    public int MoveDurationMinMs { get; set; } = 120;
    public int MoveDurationMaxMs { get; set; } = 380;

    // Randomized time until the next iteration
    public int DelayBetweenCyclesMinMs { get; set; } = 400;
    public int DelayBetweenCyclesMaxMs { get; set; } = 900;

    // Template names in the general folder (can be changed in config.json)
    public string RodReadyTemplate { get; set; } = "rod_ready.png";
    public string HookNoBaitTemplate { get; set; } = "hook_no_bait.png";
    public string HookNoBaitTemplateDestroyed { get; set; } = "hook_no_bait_destroyed.png";
    public string HookEmptyTemplate { get; set; } = "hook_empty.png";
    public string BaitTemplateName { get; set; } = "bait.png";
    public string TrueRodReadyTemplate { get; set; } = "rod_true.png";
    public string FalseRodReadyTemplate { get; set; } = "rod_false.png";
    public string StartFishingTemplate { get; set; } = "start_fishing.png";
    // Drag-and-drop offset (upward)
    public int HookEmptyYOffset { get; set; } = -45;

    // Miscellaneous
    public int OpenInvClickDownMs { get; set; } = 20;

    // Load configuration from a JSON file (default "config.json").
    // If file is missing, create one with default values.
    public static ConfigData Load(string path = "config.json")
    {
        try
        {
            if (!File.Exists(path))
            {
                var defaultCfg = new ConfigData();
                Save(defaultCfg, path);
                Console.WriteLine($"Config file '{path}' not found. A sample has been created.");
                return defaultCfg;
            }

            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cfg = JsonSerializer.Deserialize<ConfigData>(json, options) ?? new ConfigData();

            // Simple validation: ensure min <= max
            if (cfg.HoldLeftMinMs > cfg.HoldLeftMaxMs) cfg.HoldLeftMaxMs = cfg.HoldLeftMinMs;
            if (cfg.DelayAfterHoldMinMs > cfg.DelayAfterHoldMaxMs) cfg.DelayAfterHoldMaxMs = cfg.DelayAfterHoldMinMs;
            if (cfg.DelayBetweenCyclesMinMs > cfg.DelayBetweenCyclesMaxMs) cfg.DelayBetweenCyclesMaxMs = cfg.DelayBetweenCyclesMinMs;

            return cfg;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading '{path}': {ex.Message}");
            ConsoleSound.PlaySound(SoundType.Error);
            Console.WriteLine("Using default configuration values.");
            var defaultCfg = new ConfigData();
            try { Save(defaultCfg, path); } catch { }
            return defaultCfg;
        }
    }

    // Save configuration to a JSON file
    public static void Save(ConfigData cfg, string path = "config.json")
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(cfg, options);
        File.WriteAllText(path, json);
    }
}
