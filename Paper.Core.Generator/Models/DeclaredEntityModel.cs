namespace Piclip.Generator.Models;

internal record struct DeclaredEntityModel(string TypeName, string Namespace, EquatableArray<(string Namespace, string Name)> PropertyTypes);