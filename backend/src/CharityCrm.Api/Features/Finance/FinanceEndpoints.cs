using CharityCrm.Api.Setup;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Payments;
using CharityCrm.Core.Services.Settings;

namespace CharityCrm.Api.Features.Finance;

public static class FinanceEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        // Cashiers need the fees to prefill payments (FR-PAY-01), only admins may change them.
        group.MapGet("/", (SettingsService settings) => settings.GetAsync())
            .RequireAuthorization(Policies.Finance);

        group.MapPut("/", (UpdateSettingsRequest request, SettingsService settings) => settings.UpdateAsync(request))
            .RequireAuthorization(Policies.AdminOnly)
            .WithValidation<UpdateSettingsRequest>();
    }

    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments").RequireAuthorization(Policies.Finance);

        group.MapGet("/", ([AsParameters] PaymentListQuery query, PaymentService payments) => payments.ListAsync(query));

        group.MapPost("/", async (RecordPaymentRequest request, PaymentService payments, CancellationToken ct) =>
            {
                var result = await payments.RecordAsync(request, ct);
                return Results.Created($"/api/invoices/{result.InvoiceId}", result);
            })
            .WithValidation<RecordPaymentRequest>();

        var member = app.MapGroup("/api/members/{id:guid}").WithTags("Members").RequireAuthorization(Policies.Finance);
        member.MapGet("/payments", (Guid id, PaymentService payments) => payments.ListForMemberAsync(id));
        member.MapGet("/invoices", (Guid id, InvoiceService invoices) => invoices.ListForMemberAsync(id));
    }

    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices").WithTags("Invoices").RequireAuthorization(Policies.Finance);

        group.MapGet("/", ([AsParameters] InvoiceListQuery query, InvoiceService invoices) => invoices.ListAsync(query));

        group.MapGet("/{id:guid}", (Guid id, InvoiceService invoices) => invoices.GetAsync(id));

        group.MapGet("/{id:guid}/pdf", async (Guid id, InvoiceService invoices, CancellationToken ct) =>
        {
            var pdf = await invoices.GetPdfAsync(id, ct);
            return Results.File(pdf.Content, "application/pdf", pdf.FileName);
        });

        group.MapPost("/{id:guid}/cancel", (Guid id, CancelInvoiceRequest request, InvoiceService invoices, CancellationToken ct) =>
                invoices.CancelAsync(id, request, ct))
            .WithValidation<CancelInvoiceRequest>();
    }
}
