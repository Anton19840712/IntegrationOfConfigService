using Application.DTOs.Requests;
using FluentValidation;

namespace Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Логин обязателен")
                .MaximumLength(100).WithMessage("Логин не может быть длиннее 100 символов");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен")
                .MinimumLength(4).WithMessage("Пароль должен содержать минимум 4 символа");
        }
    }
}
