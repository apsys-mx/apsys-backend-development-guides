using FastEndpoints;
using FluentResults;
using System.Linq.Expressions;
using System.Net;

namespace {ProjectName}.webapi.features;

/// <summary>
/// Base endpoint with helpers for error handling.
/// </summary>
public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    /// <summary>
    /// Returns the provided message or a default error message if the input is null or empty.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The original message or the default unexpected error message.</returns>
    protected string GetMessageOrDefault(string? message) =>
        string.IsNullOrEmpty(message) ? UnexpectedErrorMessage : message;

    /// <summary>
    /// Helper for property-based error handling.
    /// </summary>
    protected async Task HandleErrorAsync(
        Expression<Func<TRequest, object?>> property,
        string message,
        HttpStatusCode status,
        CancellationToken ct)
    {
        Logger.LogWarning(message);
        AddError(property, message);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }

    /// <summary>
    /// Helper for unexpected error handling.
    /// </summary>
    /// <param name="error">The error to handle.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <param name="status">HTTP status code to return.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleUnexpectedErrorAsync(
        IError? error,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var exception = ExtractExceptionFromError(error);
        await LogAndSendErrorAsync(exception, UnexpectedErrorMessage, status, ct);
    }

    /// <summary>
    /// Helper for unexpected error handling with custom message.
    /// </summary>
    /// <param name="error">The error to handle.</param>
    /// <param name="message">The message error to handle.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <param name="status">HTTP status code to return.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleErrorWithMessageAsync(
        IError? error,
        string message,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var errorMessage = GetMessageOrDefault(message);
        var exception = ExtractExceptionFromError(error);
        await LogAndSendErrorAsync(exception, errorMessage, status, ct);
    }

    /// <summary>
    /// Handles unexpected errors that occur during request processing.
    /// </summary>
    /// <param name="ex">Exception that was thrown.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <param name="status">HTTP status code to return.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleUnexpectedErrorAsync(
        Exception? ex,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        await LogAndSendErrorAsync(ex, UnexpectedErrorMessage, status, ct);
    }

    /// <summary>
    /// Extracts an exception from a FluentResults IError if present.
    /// </summary>
    private static Exception? ExtractExceptionFromError(IError? error)
    {
        if (error?.Metadata?.TryGetValue("Exception", out var exObj) == true && exObj is Exception ex)
            return ex;
        return null;
    }

    /// <summary>
    /// Logs an error and sends the error response.
    /// </summary>
    private async Task LogAndSendErrorAsync(
        Exception? ex,
        string message,
        HttpStatusCode status,
        CancellationToken ct)
    {
        if (ex != null)
            Logger.LogError(ex, message);
        else
            Logger.LogError(message);

        AddError(message);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }
}
