using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users
{
    public class ChangeUserPasswordDtoValidator : AbstractValidator<ChangeUserPasswordDto>
    {
        public ChangeUserPasswordDtoValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Старый пароль не может быть пустым.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Новый пароль не может быть пустым.")
                .MinimumLength(8).WithMessage("Новый пароль должен содержать минимум 8 символов.")
                .Matches("[A-Z]").WithMessage("Новый пароль должен содержать хотя бы одну заглавную букву.")
                .Matches("[a-z]").WithMessage("Новый пароль должен содержать хотя бы одну строчную букву.")
                .Matches("[0-9]").WithMessage("Новый пароль должен содержать хотя бы одну цифру.")
                .NotEqual(x => x.OldPassword).WithMessage("Новый пароль не должен совпадать со старым.");
        }
    }
}
