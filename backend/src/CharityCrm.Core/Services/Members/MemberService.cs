using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Members;

/// <summary>Member records (FR-MEM-01..07).</summary>
public sealed class MemberService(IAppDbContext db, IAuditLogger audit, IClock clock)
{
    public async Task<PagedResult<MemberListItemDto>> ListAsync(MemberListQuery query)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);
        var members = Filter(query);

        var total = await members.CountAsync();
        var items = await Sort(members)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MemberListItemDto(m.Id, m.MemberNumber, m.FirstName, m.LastName, m.City, m.Email,
                m.BillingInterval, m.Status))
            .ToListAsync();

        return new PagedResult<MemberListItemDto>(items, page, pageSize, total);
    }

    public async Task<MemberDto> GetAsync(Guid id) => ToDto(await FindAsync(id, track: false));

    /// <summary>FR-MEM-01: same first name, last name and date of birth counts as a potential duplicate.</summary>
    public async Task<IReadOnlyList<DuplicateMemberDto>> FindDuplicatesAsync(DuplicateCheckRequest request, Guid? excludeId = null)
    {
        var firstName = request.FirstName.Trim().ToLower();
        var lastName = request.LastName.Trim().ToLower();
        return await db.Members.AsNoTracking()
            .Where(m => m.Id != excludeId
                && m.FirstName.ToLower() == firstName
                && m.LastName.ToLower() == lastName
                && m.DateOfBirth == request.DateOfBirth)
            .OrderBy(m => m.MemberNumber)
            .Select(m => new DuplicateMemberDto(m.Id, m.MemberNumber, m.FirstName, m.LastName, m.City, m.Status))
            .ToListAsync();
    }

    public async Task<MemberDto> CreateAsync(CreateMemberRequest request)
    {
        if (!request.ConfirmDuplicate)
        {
            var duplicates = await FindDuplicatesAsync(new DuplicateCheckRequest(request.FirstName, request.LastName, request.DateOfBirth));
            if (duplicates.Count > 0)
            {
                throw new ConflictException(
                    $"Es gibt bereits ein Mitglied mit gleichem Namen und Geburtsdatum ({string.Join(", ", duplicates.Select(d => d.MemberNumber))}).",
                    "DUPLICATE_MEMBER");
            }
        }

        var now = clock.UtcNow;
        var member = new Member
        {
            Id = Guid.NewGuid(),
            Status = MemberStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        Apply(member, request);

        await using var tx = await db.BeginTransactionAsync();
        db.Members.Add(member);
        await db.SaveChangesAsync(); // generates the member number

        audit.Add(AuditActions.MemberCreated, AuditEntityTypes.Member, member.Id.ToString(), new AuditChanges()
            .Set("memberNumber", member.MemberNumber)
            .Set("firstName", member.FirstName)
            .Set("lastName", member.LastName)
            .Set("billingInterval", member.BillingInterval)
            .Set("joinDate", member.JoinDate));
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return ToDto(member);
    }

    /// <summary>FR-MEM-04: every field except the member number; an interval change only affects future payments.</summary>
    public async Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await FindAsync(id, track: true);
        db.SetOriginalRowVersion(member, request.RowVersion);

        var before = ToDto(member);
        Apply(member, request);
        member.UpdatedAt = clock.UtcNow;

        var changes = new AuditChanges()
            .Track("firstName", before.FirstName, member.FirstName)
            .Track("lastName", before.LastName, member.LastName)
            .Track("dateOfBirth", before.DateOfBirth, member.DateOfBirth)
            .Track("street", before.Street, member.Street)
            .Track("postalCode", before.PostalCode, member.PostalCode)
            .Track("city", before.City, member.City)
            .Track("country", before.Country, member.Country)
            .Track("email", before.Email, member.Email)
            .Track("phone", before.Phone, member.Phone)
            .Track("billingInterval", before.BillingInterval, member.BillingInterval)
            .Track("joinDate", before.JoinDate, member.JoinDate)
            .Track("notes", before.Notes, member.Notes);
        if (!changes.IsEmpty)
        {
            audit.Add(AuditActions.MemberUpdated, AuditEntityTypes.Member, member.Id.ToString(), changes);
        }

        await db.SaveChangesAsync();
        return ToDto(member);
    }

    /// <summary>FR-MEM-05: members with history are not deleted but set to inactive.</summary>
    public async Task<MemberDto> DeregisterAsync(Guid id, DeregisterMemberRequest request)
    {
        var member = await FindAsync(id, track: true);
        if (member.Status == MemberStatus.Inactive)
        {
            throw new BusinessRuleException("Das Mitglied ist bereits abgemeldet.", "ALREADY_INACTIVE");
        }

        var leaveDate = request.LeaveDate ?? clock.Today;
        if (leaveDate < member.JoinDate)
        {
            throw new InputValidationException("leaveDate", "Das Austrittsdatum darf nicht vor dem Eintrittsdatum liegen.");
        }

        member.Status = MemberStatus.Inactive;
        member.LeaveDate = leaveDate;
        member.LeaveReason = request.Reason.TrimToNull();
        member.UpdatedAt = clock.UtcNow;

        audit.Add(AuditActions.MemberDeregistered, AuditEntityTypes.Member, member.Id.ToString(), new AuditChanges()
            .Track("status", MemberStatus.Active, MemberStatus.Inactive)
            .Set("leaveDate", leaveDate)
            .Set("leaveReason", member.LeaveReason));
        await db.SaveChangesAsync();
        return ToDto(member);
    }

    /// <summary>FR-MEM-06</summary>
    public async Task<MemberDto> ReactivateAsync(Guid id)
    {
        var member = await FindAsync(id, track: true);
        if (member.Status == MemberStatus.Active)
        {
            return ToDto(member);
        }

        audit.Add(AuditActions.MemberReactivated, AuditEntityTypes.Member, member.Id.ToString(), new AuditChanges()
            .Track("status", MemberStatus.Inactive, MemberStatus.Active)
            .Track("leaveDate", member.LeaveDate, null)
            .Track("leaveReason", member.LeaveReason, null));

        member.Status = MemberStatus.Active;
        member.LeaveDate = null;
        member.LeaveReason = null;
        member.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(member);
    }

    /// <summary>FR-MEM-07: CSV of the current filtered list (all pages).</summary>
    public async Task<byte[]> ExportCsvAsync(MemberListQuery query)
    {
        var members = await Sort(Filter(query)).ToListAsync();

        var csv = new CsvWriter().Row("Mitgliedsnummer", "Vorname", "Nachname", "Geburtsdatum", "Straße", "PLZ", "Ort",
            "Land", "E-Mail", "Telefon", "Zahlungsintervall", "Status", "Eintrittsdatum", "Austrittsdatum");
        foreach (var m in members)
        {
            csv.Row(m.MemberNumber, m.FirstName, m.LastName, GermanFormat.Date(m.DateOfBirth), m.Street, m.PostalCode,
                m.City, m.Country, m.Email, m.Phone, GermanFormat.Interval(m.BillingInterval), GermanFormat.Status(m.Status),
                GermanFormat.Date(m.JoinDate), GermanFormat.Date(m.LeaveDate));
        }
        return csv.ToBytes();
    }

    private IQueryable<Member> Filter(MemberListQuery query)
    {
        var members = db.Members.AsNoTracking();
        if (query.Status is { } status)
        {
            members = members.Where(m => m.Status == status);
        }
        if (query.Interval is { } interval)
        {
            members = members.Where(m => m.BillingInterval == interval);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = SearchPattern.Contains(query.Search);
            members = members.Where(m =>
                EF.Functions.ILike(m.FirstName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.LastName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.FirstName + " " + m.LastName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.LastName + " " + m.FirstName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.MemberNumber, pattern, SearchPattern.EscapeChar)
                || (m.Email != null && EF.Functions.ILike(m.Email, pattern, SearchPattern.EscapeChar)));
        }
        return members;
    }

    private static IOrderedQueryable<Member> Sort(IQueryable<Member> members) =>
        members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ThenBy(m => m.MemberNumber);

    private async Task<Member> FindAsync(Guid id, bool track)
    {
        var members = track ? db.Members : db.Members.AsNoTracking();
        return await members.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException("Mitglied nicht gefunden.");
    }

    private void Apply(Member member, IMemberInput input)
    {
        member.FirstName = input.FirstName.Trim();
        member.LastName = input.LastName.Trim();
        member.DateOfBirth = input.DateOfBirth;
        member.Street = input.Street.Trim();
        member.PostalCode = input.PostalCode.Trim();
        member.City = input.City.Trim();
        member.Country = input.Country.TrimToNull() ?? Member.DefaultCountry;
        member.Email = input.Email.TrimToNull();
        member.Phone = input.Phone.TrimToNull();
        member.BillingInterval = input.BillingInterval;
        member.JoinDate = input.JoinDate ?? clock.Today;
        member.Notes = input.Notes.TrimToNull();
    }

    internal static MemberDto ToDto(Member m) => new(
        m.Id, m.MemberNumber, m.FirstName, m.LastName, m.DateOfBirth, m.Street, m.PostalCode, m.City, m.Country,
        m.Email, m.Phone, m.BillingInterval, m.Status, m.JoinDate, m.LeaveDate, m.LeaveReason, m.Notes,
        m.CreatedAt, m.UpdatedAt, m.RowVersion);
}
