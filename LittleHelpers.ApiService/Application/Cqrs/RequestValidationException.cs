namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class RequestValidationException(string message) : Exception(message);
