namespace NAU.Application.Common.Interfaces;

/// <summary>Accessor for the currently authenticated user (implemented over HttpContext in the API).</summary>
public interface ICurrentUser
{
    /// <summary>User id from the access token, or null when unauthenticated.</summary>
    Guid? Id { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
