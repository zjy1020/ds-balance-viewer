using System.Security.Cryptography;
using System.Text;

namespace DSBalanceViewer.Services;

public class KeyVault
{
    private readonly string _filePath;

    public KeyVault()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DSBalanceViewer");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "key.bin");
    }

    public bool KeyExists() => File.Exists(_filePath);

    public void SaveKey(string apiKey)
    {
        var plain = Encoding.UTF8.GetBytes(apiKey);
        var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, cipher);
    }

    public string? LoadKey()
    {
        if (!File.Exists(_filePath)) return null;
        try
        {
            var cipher = File.ReadAllBytes(_filePath);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void DeleteKey()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }
}
