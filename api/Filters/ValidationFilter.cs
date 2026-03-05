using System.ComponentModel.DataAnnotations;

namespace Api.Filters.Validation;

public class DataAnnotationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var arg = context.Arguments.FirstOrDefault(x => x is T);

        if (arg is null) 
            return Results.BadRequest("Request body is missing.");
        var validationContext = new ValidationContext(arg);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(arg, validationContext, validationResults, true);
        if (!isValid)
        {
            var errors = validationResults.ToDictionary(
                k => k.MemberNames.First(), 
                v => v.ErrorMessage
            );
            return Results.BadRequest(new { errors });
        }
        return await next(context);
    }
}