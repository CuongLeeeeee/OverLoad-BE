namespace OverLoad.API.Common;

/// <summary>The envelope every endpoint returns, success or failure.</summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Request processed successfully") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>Shorthand for responses that carry no data.</summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Ok(string message) => ApiResponse<object?>.Ok(null, message);

    public static ApiResponse<object?> Fail(string message, IReadOnlyList<string>? errors = null) =>
        ApiResponse<object?>.Fail(message, errors);
}
