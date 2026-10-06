## Naming conventions

Follow conventional, idiomatic .NET/C# naming conventions.
Do not invent custom naming schemes unless explicitly required by
architecture.md, components.md, an ADR, or this file.

### Projects

Module projects use:

`SupportFlow.Modules.<ModuleName>`

Examples:

- `SupportFlow.Modules.Conversations`
- `SupportFlow.Modules.Identity`
- `SupportFlow.Modules.AIAssistance`

### Namespaces

Namespaces follow the project and folder structure:

- `SupportFlow.Modules.<ModuleName>.Domain`
- `SupportFlow.Modules.<ModuleName>.Application`
- `SupportFlow.Modules.<ModuleName>.Infrastructure`
- `SupportFlow.Modules.<ModuleName>.Endpoints`
- `SupportFlow.Modules.<ModuleName>.Contracts`

Do not use alternative forms such as:

`SupportFlow.<ModuleName>.Domain`

### Modules

Use concise, domain-oriented PascalCase names:

- `Conversations`
- `SupportOrganization`
- `Identity`
- `AIAssistance`
- `Notifications`
- `Audit`
- `Reporting`

Do not add suffixes such as `Module` unless explicitly required.

### C# and test naming

Use standard PascalCase for types and methods.

Do not use snake_case or underscore-separated method names.

Examples:

- `DomainArchitectureTests`
- `Should_Not_Depend_On_Outer_Layers_Or_Frameworks`
- `Should_Not_Depend_On_Other_Modules`

Test names should describe the behavior or architectural rule being verified.
