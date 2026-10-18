namespace CharityCrm.Core.Abstractions;

public static class AuditActions
{
    public const string LoginSucceeded = "Auth.Login";
    public const string LoginFailed = "Auth.LoginFailed";
    public const string Logout = "Auth.Logout";
    public const string PasswordChanged = "Auth.PasswordChanged";

    public const string UserCreated = "User.Created";
    public const string UserUpdated = "User.Updated";
    public const string UserDeactivated = "User.Deactivated";
    public const string UserReactivated = "User.Reactivated";
    public const string UserPasswordReset = "User.PasswordReset";

    public const string MemberCreated = "Member.Created";
    public const string MemberUpdated = "Member.Updated";
    public const string MemberDeregistered = "Member.Deregistered";
    public const string MemberReactivated = "Member.Reactivated";
}

public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Member = "Member";
}
