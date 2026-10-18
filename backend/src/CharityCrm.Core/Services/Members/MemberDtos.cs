using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Members;

public sealed record MemberListItemDto(
    Guid Id,
    string MemberNumber,
    string FirstName,
    string LastName,
    string City,
    string? Email,
    BillingInterval BillingInterval,
    MemberStatus Status);

public sealed record MemberDto(
    Guid Id,
    string MemberNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string? Email,
    string? Phone,
    BillingInterval BillingInterval,
    MemberStatus Status,
    DateOnly JoinDate,
    DateOnly? LeaveDate,
    string? LeaveReason,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record MemberListQuery(
    string? Search,
    MemberStatus? Status,
    BillingInterval? Interval,
    int? Page,
    int? PageSize);

/// <summary>Shared fields of create and update (FR-MEM-01, FR-MEM-04).</summary>
public interface IMemberInput
{
    string FirstName { get; }
    string LastName { get; }
    DateOnly DateOfBirth { get; }
    string Street { get; }
    string PostalCode { get; }
    string City { get; }
    string? Country { get; }
    string? Email { get; }
    string? Phone { get; }
    BillingInterval BillingInterval { get; }
    DateOnly? JoinDate { get; }
    string? Notes { get; }
}

/// <param name="ConfirmDuplicate">Set after the user confirmed the duplicate warning (FR-MEM-01).</param>
public sealed record CreateMemberRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Street,
    string PostalCode,
    string City,
    string? Country,
    string? Email,
    string? Phone,
    BillingInterval BillingInterval,
    DateOnly? JoinDate,
    string? Notes,
    bool ConfirmDuplicate = false) : IMemberInput;

public sealed record UpdateMemberRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Street,
    string PostalCode,
    string City,
    string? Country,
    string? Email,
    string? Phone,
    BillingInterval BillingInterval,
    DateOnly? JoinDate,
    string? Notes,
    uint RowVersion) : IMemberInput;

public sealed record DuplicateCheckRequest(string FirstName, string LastName, DateOnly DateOfBirth);

public sealed record DuplicateMemberDto(Guid Id, string MemberNumber, string FirstName, string LastName, string City, MemberStatus Status);

public sealed record DeregisterMemberRequest(DateOnly? LeaveDate, string? Reason);
