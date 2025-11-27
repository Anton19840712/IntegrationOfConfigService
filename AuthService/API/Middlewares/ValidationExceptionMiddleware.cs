using FluentValidation;
using System.Net;
using System.Text.Json;

namespace API.Middlewares
{
    public class ValidationExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await HandleValidationExceptionAsync(context, ex);
            }
        }

        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errors = new System.Collections.Generic.List<object>();

            foreach (var failure in exception.Errors)
            {
                errors.Add(new
                {
                    field = failure.PropertyName,
                    message = failure.ErrorMessage
                });
            }

            var response = new
            {
                code = "VALIDATION_ERROR",
                message = "Ошибка валидации входных данных",
                errors = errors
            };

            var json = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(json);
        }
    }

}
