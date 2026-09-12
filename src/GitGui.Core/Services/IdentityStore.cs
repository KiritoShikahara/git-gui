using System.Text.Json;
using GitGui.Core.Models;

namespace GitGui.Core.Services;

/// <summary>
/// Persists the user's saved name/email identities (used for the account-switcher dropdown)
/// to a JSON file under the user's AppData folder, independent of any git config.
/// </summary>
public sealed class IdentityStore
{
    private readonly string _filePath;

    public IdentityStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitGui");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "identities.json");
    }

    public List<GitIdentityModel> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<GitIdentityModel>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Save(IEnumerable<GitIdentityModel> identities)
    {
        var json = JsonSerializer.Serialize(identities.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
