using SupportFlow.Api;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.AIAssistance;
using SupportFlow.Modules.Audit;
using SupportFlow.Modules.Conversations;
using SupportFlow.Modules.Identity;
using SupportFlow.Modules.Notifications;
using SupportFlow.Modules.Reporting;
using SupportFlow.Modules.SupportOrganization;

var builder = WebApplication.CreateBuilder(args);

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

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDatabasesAsync(app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapConversationsEndpoints();
app.MapSupportOrganizationEndpoints();
app.MapIdentityEndpoints();
app.MapAIAssistanceEndpoints();
app.MapAuditEndpoints();
app.MapReportingEndpoints();

await app.RunAsync();

/// <summary>
/// Entry point, public for <c>WebApplicationFactory</c> in API tests.
/// </summary>
public partial class Program;
