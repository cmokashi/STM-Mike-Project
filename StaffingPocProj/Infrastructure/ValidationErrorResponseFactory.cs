using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StaffingPocProj.Infrastructure;

public static partial class ValidationErrorResponseFactory
{
    private const string MissingRequiredMarker = "missing required properties including:";
    private const string FrameworkRequestRequired = "The request field is required.";

    public static IActionResult Create(ActionContext context)
    {
        var cleaned = new ModelStateDictionary();
        var hasJsonError = context.ModelState.ContainsKey("$");

        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors)
            {
                var message = string.IsNullOrEmpty(error.ErrorMessage)
                    ? error.Exception?.Message ?? "The input was not valid."
                    : error.ErrorMessage;

                foreach (var (field, text) in Translate(key, message, hasJsonError))
                    cleaned.AddModelError(field, text);
            }
        }

        // Safety net: never return an empty error list.
        if (cleaned.ErrorCount == 0)
            cleaned.AddModelError("request", "The request is not valid.");

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, cleaned);

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static IEnumerable<(string Field, string Message)> Translate(string key, string message, bool hasJsonError)
    {
        // JSON body errors are reported by System.Text.Json under the key "$" or "$.property".
        if (key == "$")
        {
            var markerIndex = message.IndexOf(MissingRequiredMarker, StringComparison.Ordinal);
            if (markerIndex >= 0)
            {
                // Only look after the marker, so the class name quoted earlier in the message is ignored.
                var names = QuotedName().Matches(message[(markerIndex + MissingRequiredMarker.Length)..]);
                foreach (Match name in names)
                    yield return (name.Groups[1].Value, $"The field '{name.Groups[1].Value}' is required.");
                yield break;
            }

            yield return ("body", "The request body is not valid JSON.");
            yield break;
        }

        if (key.StartsWith("$.", StringComparison.Ordinal))
        {
            var field = key[2..];
            yield return (field, $"The value for '{field}' is not valid.");
            yield break;
        }

        // The framework adds a second, duplicate message when the JSON body fails. Skip it.
        if (hasJsonError && key == "request" && message == FrameworkRequestRequired)
            yield break;

        // Everything else (DataAnnotations such as [Range] or [StringLength]) is already clear.
        yield return (key, message);
    }

    [GeneratedRegex("'([^']+)'")]
    private static partial Regex QuotedName();
}