using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            // Правило для Email: проверять, только если поле Email присутствует в запросе
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Указан некорректный формат Email.")
                .When(x => x.Email != null);

            // Правило для FirstName: проверять, только если поле FirstName присутствует
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Имя не может быть пустым.")
                .MaximumLength(50)
                .When(x => x.FirstName != null);

            // Правило для LastName: проверять, только если поле LastName присутствует
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Фамилия не может быть пустой.")
                .MaximumLength(50)
                .When(x => x.LastName != null);

            // Для MiddleName можно не делать проверку на NotEmpty, так как оно может быть пустым
            RuleFor(x => x.MiddleName)
                .MaximumLength(50)
                .When(x => x.MiddleName != null);
        }
    }
}
