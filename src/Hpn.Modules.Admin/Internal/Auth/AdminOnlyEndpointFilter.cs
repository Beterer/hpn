using Hpn.Modules.Identity.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.AspNetCore.Http;

namespace Hpn.Modules.Admin.Internal.Auth;

internal sealed class AdminOnlyEndpointFilter(
    ICurrentUser currentUser,
    IIdentityApi identityApi) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Results.Unauthorized();
        }

        if (!await identityApi.IsAdminAsync(userId, context.HttpContext.RequestAborted))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
