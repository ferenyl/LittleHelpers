namespace LittleHelpers.ApiService.Application.Cqrs;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>;
