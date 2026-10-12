using System.Text.Json.Serialization;
using Jex.Application.Common;

namespace Jex.WebAPI.Models;

/// <summary>Non-generic view of the envelope used by filters and middleware.</summary>
public interface IApiResponse
{
    string RequestId { get; set; }

    int Code { get; }
}

/// <summary>
/// Unified API response envelope. Every business endpoint returns HTTP 200 with this shape;
/// <see cref="Code"/> (see <see cref="ErrorCodes"/>) tells success from failure.
/// Endpoints create it with <see cref="ApiResponse.Success{T}(T, long?)"/>; the
/// <see cref="Infrastructure.ApiResponseEndpointFilter"/> fills <see cref="RequestId"/>.
/// </summary>
/// <typeparam name="T">Payload type carried in <see cref="Data"/>.</typeparam>
public sealed class ApiResponse<T> : IApiResponse
{
    /// <summary>Correlation id (W3C trace id). Also returned in the <c>X-Request-Id</c> header.</summary>
    public string RequestId { get; set; } = string.Empty;

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
    /// <summary>Successful response. <paramref name="totalCount"/> is for paged lists.</summary>
    public static ApiResponse<T> Success<T>(T? data, long? totalCount = null) =>
        new() { Data = data, TotalCount = totalCount };

    /// <summary>Successful response without a payload.</summary>
    public static ApiResponse<object> Success() => new();

    public static ApiResponse<object> Fail(string requestId, int code, string msg, IDictionary<string, string[]>? errors = null) =>
        new() { RequestId = requestId, Code = code, Msg = msg, Errors = errors };
}
