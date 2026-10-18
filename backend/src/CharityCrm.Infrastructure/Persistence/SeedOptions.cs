namespace CharityCrm.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string Section = "Seed";

    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
}
