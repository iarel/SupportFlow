using System.Reflection;

namespace SupportFlow.ArchitectureTests;

internal static class Modules
{
    public static readonly string[] Names =
    [
        "Conversations",
        "SupportOrganization",
        "Identity",
        "AIAssistance",
        "Notifications",
        "Audit",
        "Reporting",
    ];

    public static readonly string[] Frameworks =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
    ];

    public static TheoryData<string> All => new(Names);

    public static string Namespace(string module) => $"SupportFlow.Modules.{module}";

    public static Assembly Internal(string module) => Assembly.Load(Namespace(module));

    public static Assembly Contracts(string module) => Assembly.Load($"{Namespace(module)}.Contracts");

    public static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
