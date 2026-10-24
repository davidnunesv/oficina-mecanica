
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Oficina.Api.Exceptions;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Requisição inválida",
                exception.Message),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado",
                exception.Message),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflito de concorrência",
                "O recurso foi alterado por outra operação. Consulte os dados novamente."),

            DbUpdateException e when e.InnerException is SqlException { Number: 2601 or 2627 } => (
                StatusCodes.Status409Conflict,
                "Registro duplicado",
                "Já existe um registro com os dados informados."),

            DbUpdateException e when e.InnerException is SqlException { Number: 547 } => (
                StatusCodes.Status409Conflict,
                "Conflito de integridade",
                "A operação viola um relacionamento existente."),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Regra de negócio não atendida",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Não foi possível processar a solicitação.")
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Erro inesperado na API.");
        else
            logger.LogWarning(exception, "Requisição rejeitada: {StatusCode}", statusCode);

        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = context.TraceIdentifier
            }).ExecuteAsync(context);

        return true;
    }
}
