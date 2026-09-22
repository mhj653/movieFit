using System.Net;

namespace ProcessVideoAnalyzer.AI.Models;

public sealed class ApiModelUnavailableException : Exception
{
    public ApiModelUnavailableException(
        string provider,
        string model,
        HttpStatusCode statusCode,
        string message)
        : base(message)
    {
        Provider = provider;
        Model = model;
        StatusCode = statusCode;
    }

    public string Provider { get; }

    public string Model { get; }

    public HttpStatusCode StatusCode { get; }
}
