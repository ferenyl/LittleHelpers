namespace LittleHelpers.ApiService.Application.Cqrs;

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>;
