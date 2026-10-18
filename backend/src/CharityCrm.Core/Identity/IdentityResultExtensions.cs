using CharityCrm.Core.Common;
using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Core.Identity;

public static class IdentityResultExtensions
{
    /// <summary>Throws a matching domain exception if the Identity operation failed.</summary>
    public static void EnsureSucceeded(this IdentityResult result, string passwordField = "password")
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors.ToList();
        if (errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new ConflictException("Diese E-Mail-Adresse wird bereits verwendet.", "DUPLICATE_EMAIL");
        }
        if (errors.Any(e => e.Code == "ConcurrencyFailure"))
        {
            throw new ConflictException(errors.First(e => e.Code == "ConcurrencyFailure").Description, "CONCURRENCY_CONFLICT");
        }

        var field = errors.All(e => e.Code.StartsWith("Password", StringComparison.Ordinal)) ? passwordField : "general";
        throw new InputValidationException(new Dictionary<string, string[]>
        {
            [field] = errors.Select(e => e.Description).ToArray(),
        });
    }
}
