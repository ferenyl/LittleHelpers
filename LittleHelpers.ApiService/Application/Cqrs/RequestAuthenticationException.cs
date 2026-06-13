namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class RequestAuthenticationException(string message) : Exception(message);
