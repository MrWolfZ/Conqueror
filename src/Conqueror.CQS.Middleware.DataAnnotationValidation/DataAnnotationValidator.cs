using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation;

internal static class DataAnnotationValidator
{
    public static void ValidateObject(object obj)
    {
        var results = new List<ValidationResult>();

        if (TryValidateObjectRecursive(obj, results))
        {
            return;
        }

        if (results.Count == 1)
        {
            throw new ValidationException(results[0], null, obj);
        }

        var errorMessage = $"validation failed with multiple errors: {string.Join(", ", results.Select(r => r.ErrorMessage))}";
        var memberNames = results.SelectMany(r => r.MemberNames).Distinct();

        var combinedResult = new ValidationResult(errorMessage, memberNames);

        throw new ValidationException(combinedResult, null, obj);
    }

    private static bool TryValidateObjectRecursive(object obj, ICollection<ValidationResult> results)
    {
        var validationWasSuccessful = Validator.TryValidateObject(obj, new(obj), results, true);

        // Validator only supports validation of attributes on properties, not constructor parameters; however,
        // for records it is common to place validation attributes on the constructor parameters instead of on
        // properties; therefore we need some custom logic to validate records based on constructor parameters
        var constructorValidationAttributesByParamName = (from constructor in obj.GetType().GetConstructors()
                                                          from param in constructor.GetParameters()
                                                          let attributes = param.GetCustomAttributes().OfType<ValidationAttribute>().ToList()
                                                          where attributes.Any()
                                                          let paramName = param.Name?.ToUpperInvariant()
                                                          where paramName is not null
                                                          group attributes by paramName
                                                          into g
                                                          select (ParamName: g.Key, Attributes: g.SelectMany(l => l).ToList()))
            .ToDictionary(item => item.ParamName, item => item.Attributes);

        var properties = obj.GetType().GetProperties().Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0).ToList();

        foreach (var property in properties)
        {
            if (constructorValidationAttributesByParamName.TryGetValue(property.Name.ToUpperInvariant(), out var validationAttributes))
            {
                var validationContext = new ValidationContext(obj) { MemberName = property.Name };

                // do NOT inline this into the next line to prevent accidentally short-circuiting validation
                var constructorValidationWasSuccessful = Validator.TryValidateValue(property.GetValue(obj)!, validationContext, results, validationAttributes);

                validationWasSuccessful = validationWasSuccessful && constructorValidationWasSuccessful;
            }

            // do NOT inline this into the next line to prevent accidentally short-circuiting validation
            var propertyValidationWasSuccessful = TryValidateProperty(obj, property, results);

            validationWasSuccessful = validationWasSuccessful && propertyValidationWasSuccessful;
        }

        return validationWasSuccessful;
    }

    private static bool TryValidateProperty(object obj, PropertyInfo property, ICollection<ValidationResult> results)
    {
        if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
        {
            return true;
        }

        var value = property.GetValue(obj);

        if (value == null)
        {
            return true;
        }

        if (value is IEnumerable enumerable)
        {
            return TryValidateEnumerableProperty(enumerable, property, results);
        }

        var nestedResults = new List<ValidationResult>();

        if (!TryValidateObjectRecursive(value, nestedResults))
        {
            foreach (var validationResult in nestedResults)
            {
                results.Add(new(validationResult.ErrorMessage, validationResult.MemberNames.Select(x => property.Name + '.' + x)));
            }

            return false;
        }

        return true;
    }

    private static bool TryValidateEnumerableProperty(IEnumerable enumerable, PropertyInfo property, ICollection<ValidationResult> results)
    {
        var validationWasSuccessful = true;

        foreach (var enumObj in enumerable)
        {
            var nestedResults = new List<ValidationResult>();

            if (!TryValidateObjectRecursive(enumObj, nestedResults))
            {
                validationWasSuccessful = false;

                foreach (var validationResult in nestedResults)
                {
                    results.Add(new(validationResult.ErrorMessage, validationResult.MemberNames.Select(x => property.Name + '.' + x)));
                }
            }
        }

        return validationWasSuccessful;
    }
}
