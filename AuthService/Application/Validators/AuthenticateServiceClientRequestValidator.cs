using Application.DTOs.Requests;
using FluentValidation;

namespace Application.Validators
{
    public class AuthenticateServiceClientRequestValidator : AbstractValidator<AuthenticateServiceClientRequest>
    {
        public AuthenticateServiceClientRequestValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty().WithMessage("ClientId обязателен");

            RuleFor(x => x.ClientSecret)
                .NotEmpty().WithMessage("ClientSecret обязателен");
        }
    }
}
