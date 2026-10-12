namespace Jex.Application.Common;

/// <summary>
/// Business status codes carried in the API response envelope (<c>code</c> field).
/// Every response is HTTP 200; the client inspects <c>code</c> to distinguish outcomes.
/// Do not change existing values; add new codes at the end of the matching range.
/// </summary>
public static class ErrorCodes
{
    /// <summary>Operation succeeded.</summary>
    public const int Success = 0;

    /// <summary>Request failed validation (model binding, FluentValidation, malformed JSON).</summary>
    public const int Validation = 4000;

    /// <summary>Caller is not authenticated.</summary>
    public const int Unauthenticated = 4010;

    /// <summary>Caller is authenticated but not allowed to perform the operation.</summary>
    public const int Forbidden = 4030;

    /// <summary>The requested resource or route does not exist.</summary>
    public const int NotFound = 4040;

    /// <summary>The HTTP method is not supported on this route.</summary>
    public const int MethodNotAllowed = 4050;

    /// <summary>The operation conflicts with current state (duplicate key, concurrency).</summary>
    public const int Conflict = 4090;

    /// <summary>Too many requests; rate limit exceeded.</summary>
    public const int TooManyRequests = 4290;

    /// <summary>Unexpected server error.</summary>
    public const int Unexpected = 5000;
}
