namespace LittleHelpers.ApiService.Application.Cqrs;

public interface IRequestHandler<in TRequest, TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken = default);
}
