using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hpn.SharedKernel.Validation;

/// <summary>
/// Fluent opt-in for the shared validation filter so each vertical slice keeps
/// its validator wiring to a single line (backbone §3.4).
/// </summary>
public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
}
