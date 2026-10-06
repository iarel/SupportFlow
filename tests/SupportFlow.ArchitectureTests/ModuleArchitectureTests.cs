namespace SupportFlow.ArchitectureTests;

// ADR-0002: a module depends only on the Contracts of other modules.
public class ModuleArchitectureTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void ShouldNotDependOnInternalsOfOtherModules(string module)
    {
        var forbidden = Modules.Names
            .Where(other => other != module)
            .Select(Modules.Namespace)
            .ToHashSet();

        var violations = Modules.Internal(module)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && forbidden.Contains(name))
            .ToList();

        Assert.Empty(violations);
    }
}
