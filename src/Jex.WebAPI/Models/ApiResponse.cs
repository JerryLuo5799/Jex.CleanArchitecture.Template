using System.Text.Json.Serialization;

namespace Jex.WebAPI.Models;

/// <summary>
/// Unified API response envelope.
/// All endpoints return HTTP 200 with this structure.
/// </summary>
/// <typeparam name="T">Type of the payload carried in <see cref="Data"/>.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>The unique request identifier (from <c>HttpContext.TraceIdentifier</c>).</summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Business status code.
    /// 2000 = success; 4000 = validation error; 4040 = not found; 5000 = server error.
    /// </summary>
    public int Code { get; init; } = 2000;

    /// <summary>Error message. Empty on success.</summary>
    public string Msg { get; init; } = string.Empty;

    /// <summary>Response payload.</summary>
    public T? Data { get; init; }

    /// <summary>Total record count for paginated responses. Omitted when not applicable.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalCount { get; init; }

    /// <summary>Creates a successful response wrapping <paramref name="data"/>.</summary>
    public static ApiResponse<T> Success(string requestId, T? data, int? totalCount = null) =>
        new() { RequestId = requestId, Code = 2000, Msg = string.Empty, Data = data, TotalCount = totalCount };

    /// <summary>Creates an error response.</summary>
    public static ApiResponse<T> Fail(string requestId, int code, string msg) =>
        new() { RequestId = requestId, Code = code, Msg = msg, Data = default };
}
