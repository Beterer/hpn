using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Hpn.SharedKernel.Validation;

/// <summary>
/// Runs FluentValidation over the first argument of type <typeparamref name="TRequest"/>
/// before the handler executes, short-circuiting with an RFC 9457
/// <c>application/problem+json</c> validation problem on failure (backbone §8.1,
/// §11). Endpoints opt in via <see cref="ValidationFilterExtensions.WithValidation{TRequest}"/>.
/// </summary>
public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest>? _validator;

    public ValidationEndpointFilter(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_validator is not null)
        {
            var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (request is not null)
            {
                var result = await _validator.ValidateAsync(request, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
