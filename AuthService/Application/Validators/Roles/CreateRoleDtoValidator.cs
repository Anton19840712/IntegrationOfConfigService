using Application.DTOs.Roles;
using FluentValidation;

namespace Application.Validators.Roles
{
    public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
    {
        public CreateRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя роли не может быть пустым.")
                .Length(3, 100).WithMessage("Имя роли должно содержать от 3 до 100 символов.");

            // Проверяем, что сам список не null, хотя он инициализирован
            RuleFor(x => x.PrivilegeIds)
                .NotNull().WithMessage("Список ID привилегий не должен быть null.");

            // Проверяем каждый Guid в списке
            RuleForEach(x => x.PrivilegeIds)
                .NotEmpty().WithMessage("ID привилегии не может быть пустым.");
        }
    }
}
