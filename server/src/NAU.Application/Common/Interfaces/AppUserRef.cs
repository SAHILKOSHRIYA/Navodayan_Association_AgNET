using NAU.Domain.Enums;

namespace NAU.Application.Common.Interfaces;

/// <summary>
/// Read-only projection of an authenticated user, exposed to the Application layer via
/// <see cref="IAppDbContext.Users"/>. Keeps ASP.NET Identity types inside Infrastructure
/// while still allowing SQL-translatable joins (e.g. directory listings need the full name).
/// </summary>
public sealed record AppUserRef(Guid Id, string FullName, string Email, UserStatus Status, bool EmailConfirmed);
