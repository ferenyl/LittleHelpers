namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class RequestAuthorizationException(string message) : Exception(message);
