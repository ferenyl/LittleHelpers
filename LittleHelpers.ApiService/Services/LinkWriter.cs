namespace LittleHelpers.ApiService.Services;

public record Link(string Href, string Rel, string Method);

public class LinkWriter<T>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly List<(Func<T, bool> condition, Func<T, Link> factory)> _links = [];

    public LinkWriter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public LinkWriter<T> AddLink(string rel, string method, Func<T, string> href, Func<T, bool>? condition = null)
    {
        _links.Add((condition ?? (_ => true), item => new Link(href(item), rel, method)));
        return this;
    }

    public LinkWriter<T> AddLinkForRole(string role, string rel, string method, Func<T, string> href)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return AddLink(rel, method, href, _ => user?.IsInRole(role) == true);
    }

    public IEnumerable<Link> Build(T item) =>
        _links
            .Where(l => l.condition(item))
            .Select(l => l.factory(item));
}
