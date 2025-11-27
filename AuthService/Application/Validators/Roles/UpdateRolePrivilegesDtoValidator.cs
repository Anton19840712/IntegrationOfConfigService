using Application.DTOs.Roles;
using FluentValidation;

namespace Application.Validators.Roles
{
    public class UpdateRolePrivilegesDtoValidator : AbstractValidator<UpdateRolePrivilegesDto>
    {
        public UpdateRolePrivilegesDtoValidator()
        {
            RuleFor(x => x.PrivilegeIds)
                .NotNull().WithMessage("Список ID привилегий не должен быть null.");

            // Проверяем, что каждый Guid в списке не является пустым (Guid.Empty)
            RuleForEach(x => x.PrivilegeIds)
                .NotEmpty().WithMessage("ID привилегии в списке не может быть пустым.");
        }
    }
}
