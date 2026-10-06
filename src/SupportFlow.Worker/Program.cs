using SupportFlow.Modules.AIAssistance;
using SupportFlow.Modules.Audit;
using SupportFlow.Modules.Conversations;
using SupportFlow.Modules.Identity;
using SupportFlow.Modules.Notifications;
using SupportFlow.Modules.Reporting;
using SupportFlow.Modules.SupportOrganization;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddConversations(builder.Configuration)
    .AddSupportOrganization(builder.Configuration)
    .AddIdentity(builder.Configuration)
    .AddAIAssistance(builder.Configuration)
    .AddNotifications(builder.Configuration)
    .AddAudit(builder.Configuration)
    .AddReporting(builder.Configuration);

var host = builder.Build();
host.Run();
