using System.Text.Json.Serialization;
using Jex.Application.Common;

namespace Jex.WebAPI.Models;

/// <summary>
/// Unified API response envelope. Every business endpoint returns HTTP 200 with this shape;
/// <see cref="Code"/> (see <see cref="ErrorCodes"/>) tells success from failure.
/// </summary>
/// <typeparam name="T">Payload type carried in <see cref="Data"/>.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Correlation id (W3C trace id). Also returned in the <c>X-Request-Id</c> header.</summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>Business status code. 0 = success.</summary>
    public int Code { get; init; } = ErrorCodes.Success;

    /// <summary>Human-readable message. Empty on success.</summary>
    public string Msg { get; init; } = string.Empty;

    /// <summary>Payload. Null on failure.</summary>
    public T? Data { get; init; }

    /// <summary>Total record count for paged list endpoints.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TotalCount { get; init; }

    /// <summary>Validation errors grouped by member name. Only present for code 4000.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }
}

/// <summary>Factory helpers for <see cref="ApiResponse{T}"/>.</summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(string requestId, T? data, long? totalCount = null) =>
        new() { RequestId = requestId, Data = data, TotalCount = totalCount };

    public static ApiResponse<object> Fail(string requestId, int code, string msg, IDictionary<string, string[]>? errors = null) =>
        new() { RequestId = requestId, Code = code, Msg = msg, Errors = errors };
}
