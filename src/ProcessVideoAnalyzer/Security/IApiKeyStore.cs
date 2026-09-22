namespace ProcessVideoAnalyzer.Security;

public interface IApiKeyStore
{
    bool HasKey(string provider);
    string? GetKey(string provider);
    void SaveKey(string provider, string apiKey);
    void DeleteKey(string provider);
}
