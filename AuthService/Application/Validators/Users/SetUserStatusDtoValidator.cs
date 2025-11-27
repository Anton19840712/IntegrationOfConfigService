using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users
{
    public class SetUserStatusDtoValidator : AbstractValidator<SetUserStatusDto>
    {
        public SetUserStatusDtoValidator()
        {
            // Проверяем, что свойство IsActive вообще было передано в JSON
            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("Необходимо передать значение для статуса активности.");
        }
    }
}
