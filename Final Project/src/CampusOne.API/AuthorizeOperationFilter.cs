using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CampusOne.API;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var hasAllowAnonymous =
            context.MethodInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Length > 0
            ||
            context.MethodInfo.DeclaringType?
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Length > 0;

        if (hasAllowAnonymous)
            return;

        var hasAuthorize =
            context.MethodInfo
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Length > 0
            ||
            context.MethodInfo.DeclaringType?
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Length > 0;

        if (!hasAuthorize)
            return;

        operation.Security ??= [];

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    "Bearer",
                    context.Document)] = []
            });
    }
}
