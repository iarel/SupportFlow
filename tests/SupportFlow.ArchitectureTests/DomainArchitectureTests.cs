using NetArchTest.Rules;

namespace SupportFlow.ArchitectureTests;

// architecture.md §4, ADR-0013: dependencies inside a module point towards the domain.
public class DomainArchitectureTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void ShouldNotDependOnOuterLayersOrFrameworks(string module)
    {
        var root = Modules.Namespace(module);

        var result = Types.InAssembly(Modules.Internal(module))
            .That().ResideInNamespace($"{root}.Domain")
            .ShouldNot().HaveDependencyOnAny(
            [
                $"{root}.Application",
                $"{root}.Infrastructure",
                $"{root}.Endpoints",
                $"{root}.EventHandlers",
                .. Modules.Frameworks,
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, Modules.Describe(result));
    }
}
