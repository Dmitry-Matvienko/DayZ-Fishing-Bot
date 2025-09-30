using Dayz_Fishing_Bot;
using OpenCvSharp;

class TemplateCache
{
    // Folder path for templates
    public string Folder { get; }
    private Dictionary<string, Mat> templates = new(StringComparer.OrdinalIgnoreCase);
    private DateTime lastScan = DateTime.MinValue;

    public TemplateCache(string folder)
    {
        Folder = folder;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    // Reloads templates from disk if new or changed files are detected
    public void EnsureUpToDate()
    {
        var now = DateTime.UtcNow;
        // Throttle scanning to at most once per second
        if ((now - lastScan).TotalSeconds < 1.0) return;
        lastScan = now;

        var files = new List<string>();
        foreach (var ext in new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" })
        {
            files.AddRange(Directory.GetFiles(Folder, ext, SearchOption.TopDirectoryOnly));
        }

        // Keepinf track of file names
        var currentFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in files)
        {
            currentFileNames.Add(Path.GetFileName(path));
            if (templates.ContainsKey(Path.GetFileName(path))) continue;
            try
            {
                var mat = Cv2.ImRead(path, ImreadModes.Color);
                if (!mat.Empty())
                    templates[Path.GetFileName(path)] = mat;
                else
                    mat.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateCache] Failed to load '{path}': {ex.Message}");
                ConsoleSound.PlaySound(SoundType.Error);
            }
        }

        // Remove templates for files that no longer exist
        var toRemove = new List<string>();
        foreach (var name in templates.Keys)
        {
            if (!currentFileNames.Contains(name))
                toRemove.Add(name);
        }
        foreach (var name in toRemove)
        {
            templates[name].Dispose();
            templates.Remove(name);
        }
    }

    // Enumerate all cached templates (name and Mat)
    public IEnumerable<(string name, Mat mat)> GetAll()
    {
        foreach (var kv in templates)
        {
            yield return (kv.Key, kv.Value);
        }
    }

    public bool IsEmpty() => templates.Count == 0;
}
