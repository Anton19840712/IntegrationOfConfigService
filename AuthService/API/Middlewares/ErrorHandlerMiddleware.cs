using System.Net;
using System.Text.Json;
using API.Exceptions;
using Application.DTOs;
using FluentValidation;

namespace API.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                var errorResponse = new ApiErrorResponse
                {
                    TraceId = context.TraceIdentifier
                };

                // Обрабатываем разные типы исключений
                switch (exception)
                {
                    // Ошибка валидации от FluentValidation
                    case ValidationException validationException:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        errorResponse.Status = (int)HttpStatusCode.BadRequest;
                        errorResponse.Code = "validation_error";
                        errorResponse.Message = "Произошла одна или несколько ошибок валидации.";
                        errorResponse.Errors = validationException.Errors
                            .GroupBy(e => e.PropertyName, StringComparer.InvariantCultureIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                        break;

                    // Ошибка "Не найдено"
                    case KeyNotFoundException:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        errorResponse.Status = (int)HttpStatusCode.NotFound;
                        errorResponse.Code = "not_found";
                        errorResponse.Message = exception.Message;
                        break;

                    // Ошибка "Не авторизован"
                    case UnauthorizedException unauthorizedException:
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        errorResponse.Status = response.StatusCode;
                        errorResponse.Code = "unauthorized";
                        errorResponse.Message = unauthorizedException.Message;
                        _logger.LogWarning("Попытка неавторизованного доступа: {Path}", context.Request.Path);
                        break;
         
                    // Ошибка "Отказано в доступе"
                    case ForbiddenException forbiddenException:
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        errorResponse.Status = response.StatusCode;
                        errorResponse.Code = "forbidden";
                        errorResponse.Message = forbiddenException.Message;
                        _logger.LogWarning("Попытка доступа к запрещенному ресурсу: {User} -> {Path}", context.User.Identity?.Name, context.Request.Path);
                        break;
                    
                    // Все остальные ошибки - это 500
                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                        errorResponse.Code = "internal_server_error";
                        errorResponse.Message = "Произошла внутренняя ошибка сервера. Пожалуйста, попробуйте позже.";
                        _logger.LogError(exception, "Необработанное исключение: {Message}", exception.Message);
                        break;
                }

                var result = JsonSerializer.Serialize(errorResponse);
                await response.WriteAsync(result);
            }
        }
    }
}