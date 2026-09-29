namespace OverLoad.Services.Exceptions;

/// <summary>
/// Base type for expected business failures. GlobalExceptionMiddleware maps each subtype
/// to an HTTP status code, so services throw these instead of returning error envelopes.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, IReadOnlyList<string>? errors = null) : base(message)
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Errors { get; }
}

/// <summary>404 - the requested resource does not exist.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.") { }
}

/// <summary>409 - the request conflicts with existing state (duplicate email, already enrolled...).</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>400 - the request is well-formed but breaks a business rule.</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, IReadOnlyList<string>? errors = null) : base(message, errors) { }
}

/// <summary>401 - credentials or tokens are missing, invalid or expired.</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>403 - the caller is authenticated but not allowed to access the resource.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>503 - a downstream service (Gemini, PayOS...) failed or is unreachable.</summary>
public class ExternalServiceException : AppException
{
    public ExternalServiceException(string message) : base(message) { }
}
