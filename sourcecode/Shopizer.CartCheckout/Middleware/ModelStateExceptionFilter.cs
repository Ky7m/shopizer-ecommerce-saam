using Microsoft.AspNetCore.Mvc.Filters;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Middleware;

public sealed class ModelStateExceptionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;
        var quantityError = context.ModelState.Any(pair =>
            pair.Key.EndsWith("Quantity", StringComparison.OrdinalIgnoreCase) &&
            pair.Value?.Errors.Any(error => error.ErrorMessage.Contains("between", StringComparison.OrdinalIgnoreCase) ||
                error.ErrorMessage.Contains("greater", StringComparison.OrdinalIgnoreCase)) == true);
        throw new DomainException(quantityError ? "INVALID_QUANTITY" : "INVALID_REQUEST",
            quantityError ? "Quantity is outside the allowed range" : "The request does not conform to the API contract",
            quantityError ? 422 : 400);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
