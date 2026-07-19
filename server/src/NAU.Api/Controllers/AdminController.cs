using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Admin;
using NAU.Domain.Constants;
using NAU.Domain.Enums;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Admin")]
public sealed class AdminController(ISender mediator, IUserAdminService users, ICurrentUser currentUser) : ControllerBase
{
    private Guid ActorId => currentUser.Id ?? throw new ForbiddenException();
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    public sealed record SetRolesRequest(IReadOnlyList<string> Roles);
    public sealed record SetStatusRequest(UserStatus Status);

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Dashboard(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardQuery(), ct);
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserAdminDto>>>> Users(
        [FromQuery] string? query, [FromQuery] string? role, [FromQuery] UserStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await users.ListAsync(query, role, status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<UserAdminDto>>.Ok(result));
    }

    /// <summary>Assign roles — SuperAdmin only (role changes are the most sensitive action).</summary>
    [HttpPatch("users/{id:guid}/roles")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<object>>> SetRoles(Guid id, SetRolesRequest body, CancellationToken ct)
    {
        await users.SetRolesAsync(id, body.Roles, ActorId, Ip, ct);
        return Ok(ApiResponse<object>.Ok(new(), "Roles updated."));
    }

    [HttpPatch("users/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> SetStatus(Guid id, SetStatusRequest body, CancellationToken ct)
    {
        await users.SetStatusAsync(id, body.Status, ActorId, Ip, ct);
        return Ok(ApiResponse<object>.Ok(new(), $"User {body.Status}."));
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(Guid id, CancellationToken ct)
    {
        await users.SoftDeleteAsync(id, ActorId, Ip, ct);
        return Ok(ApiResponse<object>.Ok(new(), "User removed."));
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> AuditLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }

    /// <summary>Donations report as CSV (date range + optional campaign).</summary>
    [HttpGet("reports/donations")]
    public async Task<IActionResult> DonationReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? campaign, CancellationToken ct = default)
    {
        var rows = await mediator.Send(new GetDonationReportQuery(from, to, campaign), ct);

        var csv = new StringBuilder();
        csv.AppendLine("Receipt,Date,Campaign,Donor,Anonymous,Amount,Currency");
        foreach (var r in rows)
        {
            var donor = r.IsAnonymous ? "Anonymous" : Escape(r.DonorName);
            csv.AppendLine(string.Join(',',
                Escape(r.ReceiptNumber), r.CapturedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Escape(r.CampaignTitle), donor, r.IsAnonymous, r.Amount.ToString(CultureInfo.InvariantCulture), r.Currency));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"donations-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
