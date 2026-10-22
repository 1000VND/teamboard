using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamBoard.Api.Json;
using TeamBoard.Api.Responses;

namespace TeamBoard.Api.Setup;

public static class ControllerSetup
{
    // Controllers + the response conventions: JSON camelCase with enums as strings and UTC dates,
    // errors as the { success, data, message } envelope whose "errors" keys match the JSON names and whose messages are Vietnamese.
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(o =>
            {
                o.ModelMetadataDetailsProviders.Add(new VietnameseValidationMetadataProvider());

                // Errors raised by model binding (wrong type, missing body) rather than by DataAnnotations
                var m = o.ModelBindingMessageProvider;
                m.SetMissingRequestBodyRequiredValueAccessor(() => "Thiếu body của request.");
                m.SetValueMustNotBeNullAccessor(_ => "Không được để trống.");
                m.SetMissingBindRequiredValueAccessor(_ => "Không được để trống.");
                m.SetMissingKeyOrValueAccessor(() => "Không được để trống.");
                m.SetAttemptedValueIsInvalidAccessor((value, _) => $"Giá trị '{value}' không hợp lệ.");
                m.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
                m.SetUnknownValueIsInvalidAccessor(_ => "Giá trị không hợp lệ.");
                m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
                m.SetValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
                m.SetValueMustBeANumberAccessor(_ => "Phải là số.");
                m.SetNonPropertyValueMustBeANumberAccessor(() => "Phải là số.");
            })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                // Hide System.Text.Json's English exception text; errors without a message get one below
                o.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = CreateValidationResponse);

        // 404/415/… produced inside MVC become an envelope instead of ProblemDetails
        services.Replace(ServiceDescriptor.Singleton<IClientErrorFactory, ApiResponseClientErrorFactory>());

        return services;
    }

    private static IActionResult CreateValidationResponse(ActionContext context)
    {
        // When the JSON body can't be read, MVC reports both the bad field ("$.status", or "" for a missing body)
        // and the whole body parameter ("input": required). Keep only the first — it tells the client what to fix.
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet();
        var hasJsonErrors = context.ModelState.Any(e => (e.Key == "" || e.Key.StartsWith('$')) && e.Value?.Errors.Count > 0);

        // Model state keys come from C# names ("Title"); rewrite them to the JSON names ("title").
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0 || (hasJsonErrors && bodyParameters.Contains(key))) continue;

            errors[ToJsonPath(key)] = entry.Errors
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Giá trị không hợp lệ." : e.ErrorMessage)
                .ToArray();
        }

        return new BadRequestObjectResult(ApiResponse.Fail(context.HttpContext, "Dữ liệu không hợp lệ.", errors));
    }

    // "Tasks[0].Title" → "tasks[0].title"; JSON paths such as "$.status" are already camelCase;
    // "" (the body as a whole) → "$", the JSON root.
    private static string ToJsonPath(string key) => key == ""
        ? "$"
        : string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
