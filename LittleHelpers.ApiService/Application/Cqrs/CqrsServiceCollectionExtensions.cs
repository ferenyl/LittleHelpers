namespace LittleHelpers.ApiService.Application.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddDecoratedQueryHandler<TQuery, TResult, THandler>(this IServiceCollection services)
        where TQuery : notnull
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IQueryHandler<TQuery, TResult>>(sp =>
            new RequestAuthorizationDecorator<TQuery, TResult>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<IHttpContextAccessor>()));
        return services;
    }

    public static IServiceCollection AddDecoratedCommandHandler<TCommand, TResult, THandler>(this IServiceCollection services)
        where TCommand : notnull
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResult>>(sp =>
            new RequestAuthorizationDecorator<TCommand, TResult>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<IHttpContextAccessor>()));
        return services;
    }
}
