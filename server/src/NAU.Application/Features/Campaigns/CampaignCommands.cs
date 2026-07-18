using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Campaigns;

// ── Create ──────────────────────────────────────────────────────────────────

public sealed record CreateCampaignCommand(Guid AdminId, UpsertCampaignDto Data) : IRequest<Guid>;

public sealed class CreateCampaignHandler(IAppDbContext db) : IRequestHandler<CreateCampaignCommand, Guid>
{
    public async Task<Guid> Handle(CreateCampaignCommand cmd, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.IsActive, ct)
            ?? throw new DomainRuleException("No active school is configured.");

        var d = cmd.Data;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Title = d.Title.Trim(),
            Slug = await UniqueSlugAsync(db, d.Title, ct),
            Description = d.Description?.Trim(),
            GoalAmount = d.GoalAmount,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            OrganizerName = d.OrganizerName?.Trim(),
            Status = CampaignStatus.Draft,
            CreatedBy = cmd.AdminId,
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return campaign.Id;
    }

    /// <summary>Generates a unique slug, appending -2, -3, … on collision.</summary>
    internal static async Task<string> UniqueSlugAsync(IAppDbContext db, string title, CancellationToken ct, Guid? excludeId = null)
    {
        var baseSlug = Slugger.Slugify(title);
        var slug = baseSlug;
        var n = 1;
        while (await db.Campaigns.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct))
            slug = $"{baseSlug}-{++n}";
        return slug;
    }
}

// ── Update ──────────────────────────────────────────────────────────────────

public sealed record UpdateCampaignCommand(Guid Id, UpsertCampaignDto Data) : IRequest;

public sealed class UpdateCampaignHandler(IAppDbContext db) : IRequestHandler<UpdateCampaignCommand>
{
    public async Task Handle(UpdateCampaignCommand cmd, CancellationToken ct)
    {
        var c = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Campaign", cmd.Id);

        var d = cmd.Data;
        if (!string.Equals(c.Title, d.Title.Trim(), StringComparison.Ordinal))
            c.Slug = await CreateCampaignHandler.UniqueSlugAsync(db, d.Title, ct, c.Id);

        c.Title = d.Title.Trim();
        c.Description = d.Description?.Trim();
        c.GoalAmount = d.GoalAmount;
        c.StartDate = d.StartDate;
        c.EndDate = d.EndDate;
        c.OrganizerName = d.OrganizerName?.Trim();
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── Status transition ───────────────────────────────────────────────────────

public sealed record ChangeCampaignStatusCommand(Guid Id, CampaignStatus Status) : IRequest;

public sealed class ChangeCampaignStatusHandler(IAppDbContext db) : IRequestHandler<ChangeCampaignStatusCommand>
{
    public async Task Handle(ChangeCampaignStatusCommand cmd, CancellationToken ct)
    {
        var c = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Campaign", cmd.Id);

        // A campaign must have a goal and start date before it can go live.
        if (cmd.Status == CampaignStatus.Active && c.GoalAmount <= 0)
            throw new DomainRuleException("Set a goal amount before activating the campaign.");

        c.Status = cmd.Status;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── Soft delete ─────────────────────────────────────────────────────────────

public sealed record DeleteCampaignCommand(Guid Id) : IRequest;

public sealed class DeleteCampaignHandler(IAppDbContext db) : IRequestHandler<DeleteCampaignCommand>
{
    public async Task Handle(DeleteCampaignCommand cmd, CancellationToken ct)
    {
        var c = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Campaign", cmd.Id);

        var hasDonations = await db.Donations.AnyAsync(d => d.CampaignId == c.Id && d.Status == DonationStatus.Captured, ct);
        if (hasDonations)
            throw new DomainRuleException("This campaign has donations and cannot be deleted; close it instead.");

        c.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── Post an update ──────────────────────────────────────────────────────────

public sealed record PostCampaignUpdateCommand(Guid CampaignId, Guid AdminId, PostUpdateDto Data) : IRequest<Guid>;

public sealed class PostCampaignUpdateHandler(IAppDbContext db) : IRequestHandler<PostCampaignUpdateCommand, Guid>
{
    public async Task<Guid> Handle(PostCampaignUpdateCommand cmd, CancellationToken ct)
    {
        var exists = await db.Campaigns.AnyAsync(c => c.Id == cmd.CampaignId, ct);
        if (!exists) throw new NotFoundException("Campaign", cmd.CampaignId);

        var update = new CampaignUpdate
        {
            Id = Guid.NewGuid(),
            CampaignId = cmd.CampaignId,
            Title = cmd.Data.Title.Trim(),
            Body = cmd.Data.Body.Trim(),
            CreatedBy = cmd.AdminId,
        };
        db.CampaignUpdates.Add(update);
        await db.SaveChangesAsync(ct);
        return update.Id;
    }
}

// ── Set cover image ─────────────────────────────────────────────────────────

public sealed record SetCampaignCoverCommand(Guid CampaignId, Stream Content, string FileName, string ContentType) : IRequest<string>;

public sealed class SetCampaignCoverHandler(IAppDbContext db, IFileStorage storage) : IRequestHandler<SetCampaignCoverCommand, string>
{
    public async Task<string> Handle(SetCampaignCoverCommand cmd, CancellationToken ct)
    {
        var c = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == cmd.CampaignId, ct)
            ?? throw new NotFoundException("Campaign", cmd.CampaignId);

        var key = await storage.SaveAsync(cmd.Content, cmd.FileName, cmd.ContentType, "campaigns", ct);
        var old = c.CoverImageKey;
        c.CoverImageKey = key;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(old)) await storage.DeleteAsync(old, ct);
        return key;
    }
}
