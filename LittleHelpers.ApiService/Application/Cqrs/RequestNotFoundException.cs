namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class RequestNotFoundException(string message) : Exception(message);
