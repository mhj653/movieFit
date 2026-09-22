using System.Security.Cryptography;
using System.Text;

namespace ProcessVideoAnalyzer.Security;

public sealed class WindowsApiKeyStore : IApiKeyStore
{
    private readonly string _root;

    public WindowsApiKeyStore()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "Secrets");
        Directory.CreateDirectory(_root);
    }

    public bool HasKey(string provider)
    {
        return File.Exists(GetPath(provider));
    }

    public string? GetKey(string provider)
    {
        var path = GetPath(provider);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = File.ReadAllBytes(path);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    public void SaveKey(string provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(apiKey.Trim());
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(provider), protectedBytes);
    }

    public void DeleteKey(string provider)
    {
        var path = GetPath(provider);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetPath(string provider)
    {
        var safeName = string.Concat(provider.ToLowerInvariant().Where(char.IsLetterOrDigit));
        return Path.Combine(_root, $"{safeName}.bin");
    }
}
