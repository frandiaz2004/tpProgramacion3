using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Tp3prog.Filters;

public sealed class PersistenceExceptionFilter(ILogger<PersistenceExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException && !IsConstraintConflict(context.Exception)) return;

        logger.LogWarning(context.Exception, "Conflicto al guardar cambios en la base de datos.");
        context.Result = new ConflictObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "No se pudieron guardar los cambios.",
            Detail = "El registro cambió, está relacionado con otro registro o infringe una restricción de la base de datos."
        });
        context.ExceptionHandled = true;
    }

    private static bool IsConstraintConflict(Exception exception) => exception switch
    {
        // SaveChanges envuelve el error; ExecuteUpdate puede entregarlo directamente.
        DbUpdateException { InnerException: MySqlException { Number: 1062 or 1451 or 1452 } } => true,
        MySqlException { Number: 1062 or 1451 or 1452 } => true,
        _ => false
    };
}
