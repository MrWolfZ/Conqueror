using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.DataAnnotationValidation
public sealed class DataAnnotationValidationCommandMiddleware : ICommandMiddleware
{
    public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);

        ValidateRecord(ctx.Command);
        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }

    // for records with a primary constructor, ASP.NET Core requires validation attributes to be
    // defined on constructor parameters instead of properties (e.g. [param: Required] instead of
    // [property: Required]), but Validator only supports validation of attributes on properties;
    // therefore we need some custom logic to validate records based on constructor parameters
    private static void ValidateRecord(object command)
    {
        var constructorValidationAttributesByParamName = (from constructor in command.GetType().GetConstructors()
                                                          from param in constructor.GetParameters()
                                                          let attributes = param.GetCustomAttributes().OfType<ValidationAttribute>().ToList()
                                                          where attributes.Any()
                                                          group attributes by param.Name
                                                          into g
                                                          select (ParamName: g.Key, Attributes: g.SelectMany(l => l).ToList()))
            .ToDictionary(item => item.ParamName, item => item.Attributes);

        foreach (var property in command.GetType().GetProperties())
        {
            if (!constructorValidationAttributesByParamName.TryGetValue(property.Name, out var validationAttributes))
            {
                continue;
            }

            var validationContext = new ValidationContext(command) { MemberName = property.Name };
            Validator.ValidateValue(property.GetValue(command)!, validationContext, validationAttributes);
        }
    }
}
