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
    /// Helper for property-based error handling.
    /// </summary>
    protected async Task HandleErrorAsync(
        Expression<Func<TRequest, object?>> property,
        string message,
        HttpStatusCode status,
        CancellationToken ct)
    {
        this.Logger.LogWarning(message);
        AddError(property, message);
        await SendErrorsAsync(statusCode: (int)status, cancellation: ct);
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
        if (error != null && error.Metadata != null && error.Metadata.TryGetValue("Exception", out var exObj))
        {
            if (exObj is Exception ex)
                this.Logger.LogError(ex, UnexpectedErrorMessage);
            else
                this.Logger.LogError(UnexpectedErrorMessage);
        }
        else
            this.Logger.LogError(UnexpectedErrorMessage);

        AddError(UnexpectedErrorMessage);
        await SendErrorsAsync(statusCode: (int)status, cancellation: ct);
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
        if (ex != null)
            this.Logger.LogError(ex, UnexpectedErrorMessage);
        else
            this.Logger.LogError(UnexpectedErrorMessage);

        AddError(UnexpectedErrorMessage);
        await SendErrorsAsync(statusCode: (int)status, cancellation: ct);
    }
}
