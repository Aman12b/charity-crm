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

    public const string SettingsUpdated = "Settings.Updated";
    public const string PaymentRecorded = "Payment.Recorded";
    public const string PaymentCancelled = "Payment.Cancelled";
    public const string InvoiceIssued = "Invoice.Issued";
    public const string InvoiceCancelled = "Invoice.Cancelled";
    public const string InvoiceResent = "Invoice.Resent";
}

public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Member = "Member";
    public const string Settings = "OrganizationSettings";
    public const string Payment = "Payment";
    public const string Invoice = "Invoice";
}
