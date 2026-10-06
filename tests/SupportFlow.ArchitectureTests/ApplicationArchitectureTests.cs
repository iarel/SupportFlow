using NetArchTest.Rules;

namespace SupportFlow.ArchitectureTests;

// architecture.md §4, ADR-0013: Application depends on ports, not on adapters.
public class ApplicationArchitectureTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void ShouldNotDependOnAdaptersOrFrameworks(string module)
    {
        var root = Modules.Namespace(module);

        var result = Types.InAssembly(Modules.Internal(module))
            .That().ResideInNamespace($"{root}.Application")
            .ShouldNot().HaveDependencyOnAny(
            [
                $"{root}.Infrastructure",
                $"{root}.Endpoints",
                $"{root}.EventHandlers",
                .. Modules.Frameworks,
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, Modules.Describe(result));
    }
}
