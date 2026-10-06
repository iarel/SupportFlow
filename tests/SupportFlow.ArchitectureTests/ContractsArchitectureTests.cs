namespace SupportFlow.ArchitectureTests;

// architecture.md §4: Contracts depend on nothing but the BCL.
public class ContractsArchitectureTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void ShouldDependOnlyOnBaseClassLibrary(string module)
    {
        var violations = Modules.Contracts(module)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && (name.StartsWith("SupportFlow.", StringComparison.Ordinal)
                    || Modules.Frameworks.Any(framework => name.StartsWith(framework, StringComparison.Ordinal))))
            .ToList();

        Assert.Empty(violations);
    }
}
