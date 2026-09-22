using System.Net;

namespace ProcessVideoAnalyzer.AI.Models;

public sealed class ApiQuotaExceededException : Exception
{
    public ApiQuotaExceededException(string provider, HttpStatusCode statusCode, string message)
        : base(message)
    {
        Provider = provider;
        StatusCode = statusCode;
    }

    public string Provider { get; }
    public HttpStatusCode StatusCode { get; }
}
