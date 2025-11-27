using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users
{
    public class UpdateUserRolesDtoValidator : AbstractValidator<UpdateUserRolesDto>
    {
        public UpdateUserRolesDtoValidator()
        {
            RuleFor(x => x.RoleIds)
                .NotNull().WithMessage("Список ID ролей не должен быть null.");

            // Проверяем, что в списке нет пустых Guid
            RuleForEach(x => x.RoleIds)
                .NotEmpty().WithMessage("ID роли не может быть пустым.");
        }
    }
}
