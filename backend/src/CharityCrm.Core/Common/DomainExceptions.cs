namespace CharityCrm.Core.Common;

/// <summary>
/// Base class for expected business errors. Messages are shown to the user and therefore German.
/// The API maps each subtype to an RFC 7807 ProblemDetails response.
/// </summary>
public abstract class DomainException(string message, string code) : Exception(message)
{
    /// <summary>Machine-readable error code for the frontend (e.g. "DUPLICATE_PERIOD").</summary>
    public string Code { get; } = code;
}

/// <summary>Maps to 404.</summary>
public sealed class NotFoundException(string message, string code = "NOT_FOUND")
    : DomainException(message, code);

/// <summary>Maps to 409 – the request conflicts with the current state (duplicates, overlaps, stale versions).</summary>
public sealed class ConflictException(string message, string code = "CONFLICT")
    : DomainException(message, code);

/// <summary>Maps to 422 – a business rule forbids the action (e.g. last admin, inactive member).</summary>
public sealed class BusinessRuleException(string message, string code = "BUSINESS_RULE")
    : DomainException(message, code);

/// <summary>Maps to 400 – input is invalid for a single field (used where FluentValidation cannot check, e.g. Identity errors).</summary>
public sealed class InputValidationException(IDictionary<string, string[]> errors)
    : DomainException("Die Eingaben sind ungültig.", "VALIDATION")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public InputValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }
}

/// <summary>Maps to 401 – login failed. The message never reveals which field was wrong (FR-AUTH-01).</summary>
public sealed class AuthenticationFailedException(string message, string code = "INVALID_CREDENTIALS")
    : DomainException(message, code);
