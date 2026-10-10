using Microsoft.AspNetCore.Diagnostics;
using SupportFlow.Api;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.AIAssistance;
using SupportFlow.Modules.Audit;
using SupportFlow.Modules.Conversations;
using SupportFlow.Modules.Identity;
using SupportFlow.Modules.Notifications;
using SupportFlow.Modules.Reporting;
using SupportFlow.Modules.SupportOrganization;
using SupportFlow.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ADR-0011: Bearer JWT of the external Identity Provider, settings in Authentication:Schemes:Bearer. Claims keep
// their JWT names ("sub"), which the Identity module maps to the user account (ADR-0016).
builder.Services.AddAuthentication().AddJwtBearer(options => options.MapInboundClaims = false);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

builder.Services
    .AddConversations(builder.Configuration)
    .AddSupportOrganization(builder.Configuration)
    .AddIdentity(builder.Configuration)
    .AddAIAssistance(builder.Configuration)
    .AddNotifications(builder.Configuration)
    .AddAudit(builder.Configuration)
    .AddReporting(builder.Configuration);

var app = builder.Build();

// ADR-0017: outside Development the schema is changed only by `migrate`, a separate deployment step that runs
// before the new version starts and exits.
if (args is ["migrate"])
{
    await app.Services.MigrateDatabasesAsync(CancellationToken.None);
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDatabasesAsync(app.Lifetime.ApplicationStopping);
}

// Exceptions turned into responses by ApplicationExceptionHandler (422, 429, 503, …) are expected outcomes, not
// errors to log.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = context => context.ExceptionHandledBy == ExceptionHandledType.ExceptionHandlerService,
});
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();

app.MapConversationsEndpoints();
app.MapSupportOrganizationEndpoints();
app.MapIdentityEndpoints();
app.MapAIAssistanceEndpoints();
app.MapAuditEndpoints();
app.MapReportingEndpoints();

await app.RunAsync();
