using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Login).NotEmpty().Length(3, 50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Пароль должен содержать заглавную букву.")
                .Matches("[a-z]").WithMessage("Пароль должен содержать строчную букву.")
                .Matches("[0-9]").WithMessage("Пароль должен содержать цифру.");
        }
    }
}
