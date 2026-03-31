using System.Security.Claims;
using LittleHelpers.ApiService.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace LittleHelpers.Tests.UnitTests;

public class LinkWriterTests
{
    private static IHttpContextAccessor MakeAccessor(string? role = null)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();
        if (role is not null)
        {
            ctx.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
        }
        accessor.HttpContext.Returns(ctx);
        return accessor;
    }

    [Fact]
    public void AddLink_AlwaysIncludesLink()
    {
        var lw = new LinkWriter<string>(MakeAccessor())
            .AddLink("self", "GET", _ => "/resource/1");

        var links = lw.Build("item").ToList();

        Assert.Single(links);
        Assert.Equal("self", links[0].Rel);
        Assert.Equal("GET", links[0].Method);
        Assert.Equal("/resource/1", links[0].Href);
    }

    [Fact]
    public void AddLink_WithConditionFalse_ExcludesLink()
    {
        var lw = new LinkWriter<string>(MakeAccessor())
            .AddLink("delete", "DELETE", _ => "/resource/1", _ => false);

        var links = lw.Build("item").ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void AddLinkForRole_MatchingRole_IncludesLink()
    {
        var lw = new LinkWriter<string>(MakeAccessor(role: "Parent"))
            .AddLinkForRole("Parent", "edit", "PUT", _ => "/resource/1");

        var links = lw.Build("item").ToList();

        Assert.Single(links);
        Assert.Equal("edit", links[0].Rel);
    }

    [Fact]
    public void AddLinkForRole_NonMatchingRole_ExcludesLink()
    {
        var lw = new LinkWriter<string>(MakeAccessor(role: "Child"))
            .AddLinkForRole("Parent", "edit", "PUT", _ => "/resource/1");

        var links = lw.Build("item").ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void AddLinkForRole_NoUser_ExcludesLink()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var lw = new LinkWriter<string>(accessor)
            .AddLinkForRole("Parent", "edit", "PUT", _ => "/resource/1");

        var links = lw.Build("item").ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void Build_HrefUsesItemValue()
    {
        var lw = new LinkWriter<int>(MakeAccessor())
            .AddLink("self", "GET", id => $"/resource/{id}");

        var links = lw.Build(42).ToList();

        Assert.Equal("/resource/42", links[0].Href);
    }

    [Fact]
    public void MultipleLinks_AllIncluded_WhenConditionsMet()
    {
        var lw = new LinkWriter<string>(MakeAccessor(role: "Parent"))
            .AddLink("self", "GET", _ => "/r")
            .AddLinkForRole("Parent", "edit", "PUT", _ => "/r")
            .AddLinkForRole("Parent", "delete", "DELETE", _ => "/r");

        var links = lw.Build("x").ToList();

        Assert.Equal(3, links.Count);
    }
}
