using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Admin;

public sealed record DashboardCardsDto(
    int RegisteredAlumni, int VerifiedAlumni, int PendingVerifications,
    int ActiveCampaigns, decimal FundsRaised, int UpcomingEvents, int TotalDonations);

public sealed record TimeSeriesPointDto(string Label, decimal Value);

public sealed record VerificationBreakdownDto(int Pending, int Approved, int Rejected);

public sealed record DashboardDto(
    DashboardCardsDto Cards,
    IReadOnlyList<TimeSeriesPointDto> MonthlyDonations,
    IReadOnlyList<TimeSeriesPointDto> RegistrationTrend,
    VerificationBreakdownDto Verification);

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler(IAppDbContext db) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var captured = db.Donations.Where(d => d.Status == DonationStatus.Captured);

        var cards = new DashboardCardsDto(
            RegisteredAlumni: await db.AlumniProfiles.CountAsync(ct),
            VerifiedAlumni: await db.AlumniProfiles.CountAsync(p => p.IsVerified, ct),
            PendingVerifications: await db.VerificationRequests.CountAsync(r => r.Status == VerificationStatus.Pending, ct),
            ActiveCampaigns: await db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Active, ct),
            FundsRaised: await captured.SumAsync(d => (decimal?)d.Amount, ct) ?? 0m,
            UpcomingEvents: await db.Events.CountAsync(e => e.Status == EventStatus.Published && e.EventDate >= now, ct),
            TotalDonations: await captured.CountAsync(ct));

        // Last 6 months of captured donations, bucketed by month.
        var since = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var donationBuckets = await captured
            .Where(d => d.CapturedAt >= since)
            .GroupBy(d => new { d.CapturedAt!.Value.Year, d.CapturedAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var registrationBuckets = await db.AlumniProfiles
            .Where(p => p.CreatedAt >= since)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var months = Enumerable.Range(0, 6).Select(i => since.AddMonths(i)).ToList();
        var monthlyDonations = months.Select(m => new TimeSeriesPointDto(
            m.ToString("MMM yy"),
            donationBuckets.FirstOrDefault(b => b.Year == m.Year && b.Month == m.Month)?.Sum ?? 0m)).ToList();
        var registrationTrend = months.Select(m => new TimeSeriesPointDto(
            m.ToString("MMM yy"),
            registrationBuckets.FirstOrDefault(b => b.Year == m.Year && b.Month == m.Month)?.Count ?? 0)).ToList();

        var verification = new VerificationBreakdownDto(
            await db.VerificationRequests.CountAsync(r => r.Status == VerificationStatus.Pending, ct),
            await db.VerificationRequests.CountAsync(r => r.Status == VerificationStatus.Approved, ct),
            await db.VerificationRequests.CountAsync(r => r.Status == VerificationStatus.Rejected, ct));

        return new DashboardDto(cards, monthlyDonations, registrationTrend, verification);
    }
}
