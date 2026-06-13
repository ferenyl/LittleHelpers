namespace LittleHelpers.ApiService.Application.Cqrs;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireRolesAttribute(params string[] roles) : Attribute
{
    public IReadOnlyCollection<string> Roles { get; } = roles;
}
