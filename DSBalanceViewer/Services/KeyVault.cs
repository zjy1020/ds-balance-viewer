using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DSBalanceViewer.Services;

public class KeyVault
{
    private readonly string _filePath;
    private List<KeyEntry> _keys;

    public KeyVault()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DSBalanceViewer");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "keys.json");
        _keys = LoadAll();
    }

    public List<KeyEntry> ListKeys() => _keys;

    public string? GetActiveKey()
    {
        var active = _keys.FirstOrDefault(k => k.IsActive);
        if (active == null) return null;
        try
        {
            var cipher = Convert.FromBase64String(active.EncryptedKey);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    public void SaveKey(string name, string apiKey)
    {
        var plain = Encoding.UTF8.GetBytes(apiKey);
        var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        var encrypted = Convert.ToBase64String(cipher);

        // deactivate all existing
        foreach (var k in _keys) k.IsActive = false;

        // add or update
        var existing = _keys.FirstOrDefault(k => k.Name == name);
        if (existing != null)
        {
            existing.EncryptedKey = encrypted;
            existing.IsActive = true;
        }
        else
        {
            _keys.Add(new KeyEntry { Name = name, EncryptedKey = encrypted, IsActive = true });
        }

        Persist();
    }

    public void SetActive(string name)
    {
        foreach (var k in _keys) k.IsActive = (k.Name == name);
        Persist();
    }

    public void DeleteKey(string name)
    {
        _keys.RemoveAll(k => k.Name == name);
        if (_keys.Count > 0 && !_keys.Any(k => k.IsActive))
            _keys[0].IsActive = true;
        Persist();
    }

    public bool AnyKeyExists() => _keys.Count > 0;

    private List<KeyEntry> LoadAll()
    {
        if (!File.Exists(_filePath)) return new();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<KeyEntry>>(json) ?? new();
        }
        catch { return new(); }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_keys);
        File.WriteAllText(_filePath, json);
    }
}

public class KeyEntry
{
    public string Name { get; set; } = "";
    public string EncryptedKey { get; set; } = "";
    public bool IsActive { get; set; }
}
