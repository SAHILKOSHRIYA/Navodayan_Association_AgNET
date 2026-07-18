using NAU.Application.Features.Profiles;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.UnitTests.Profiles;

public class ProfilePrivacyFilterTests
{
    private static AlumniProfile Sample(SectionVisibility contact, SectionVisibility prof, SectionVisibility acad) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Batch = 2019,
        House = "Aravali",
        Mobile = "9999999999",
        Address = "Raipur",
        CurrentCity = "Bengaluru",
        Company = "Acme",
        Designation = "SDE",
        Privacy = new ProfilePrivacy { Contact = contact, Professional = prof, Academic = acad },
        Skills = [new Skill { Name = "csharp" }],
    };

    [Fact]
    public void Anonymous_viewer_sees_only_public_sections()
    {
        var p = Sample(SectionVisibility.Members, SectionVisibility.Members, SectionVisibility.Public);

        var dto = ProfilePrivacyFilter.Apply(p, "Rahul", viewerId: null, viewerIsAdmin: false);

        Assert.Equal(2019, dto.Batch);       // academic = public → visible
        Assert.Null(dto.Company);            // professional = members → hidden from anonymous
        Assert.Null(dto.Mobile);             // contact = members → hidden
        Assert.Single(dto.Skills);           // skills always visible
    }

    [Fact]
    public void Member_viewer_sees_members_sections_but_not_private()
    {
        var p = Sample(SectionVisibility.Private, SectionVisibility.Members, SectionVisibility.Public);

        var dto = ProfilePrivacyFilter.Apply(p, "Rahul", viewerId: Guid.NewGuid(), viewerIsAdmin: false);

        Assert.Equal("Acme", dto.Company);   // members section, member viewer → visible
        Assert.Equal("Bengaluru", dto.CurrentCity);
        Assert.Null(dto.Mobile);             // contact = private → hidden even from members
    }

    [Fact]
    public void Owner_sees_everything_regardless_of_settings()
    {
        var p = Sample(SectionVisibility.Private, SectionVisibility.Private, SectionVisibility.Private);

        var dto = ProfilePrivacyFilter.Apply(p, "Rahul", viewerId: p.UserId, viewerIsAdmin: false);

        Assert.Equal(2019, dto.Batch);
        Assert.Equal("Acme", dto.Company);
        Assert.Equal("9999999999", dto.Mobile);
    }

    [Fact]
    public void Admin_sees_everything()
    {
        var p = Sample(SectionVisibility.Private, SectionVisibility.Private, SectionVisibility.Private);

        var dto = ProfilePrivacyFilter.Apply(p, "Rahul", viewerId: Guid.NewGuid(), viewerIsAdmin: true);

        Assert.Equal("9999999999", dto.Mobile);
        Assert.Equal("Acme", dto.Company);
    }
}
