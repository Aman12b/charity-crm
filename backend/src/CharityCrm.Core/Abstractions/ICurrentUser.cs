namespace CharityCrm.Core.Abstractions;

/// <summary>The authenticated user of the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
