namespace NAU.Application.Common.Exceptions;

/// <summary>Requested entity does not exist → HTTP 404.</summary>
public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

/// <summary>Caller is authenticated but not allowed → HTTP 403.</summary>
public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : Exception(message);

/// <summary>State conflict (duplicate email, already processed, …) → HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>A domain rule was violated → HTTP 422.</summary>
public sealed class DomainRuleException(string message) : Exception(message);
