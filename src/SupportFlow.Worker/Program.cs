using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.AIAssistance;
using SupportFlow.Modules.Audit;
using SupportFlow.Modules.Conversations;
using SupportFlow.Modules.Identity;
using SupportFlow.Modules.Notifications;
using SupportFlow.Modules.Reporting;
using SupportFlow.Modules.SupportOrganization;
using SupportFlow.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddConversations(builder.Configuration)
    .AddSupportOrganization(builder.Configuration)
    .AddIdentity(builder.Configuration)
    .AddAIAssistance(builder.Configuration)
    .AddNotifications(builder.Configuration)
    .AddAudit(builder.Configuration)
    .AddReporting(builder.Configuration);

// Modules register their outboxes, handlers and cleanups; the Worker runs them (ADR-0003). The schema is
// migrated by the API in Development; the Worker does not migrate.
builder.Services.TryAddSingleton(new OutboxDispatcherOptions());
builder.Services.TryAddSingleton(new RetentionOptions());
builder.Services.AddHostedService<OutboxDispatcherService>();
builder.Services.AddHostedService<RetentionCleanupService>();

var host = builder.Build();
await host.RunAsync();
