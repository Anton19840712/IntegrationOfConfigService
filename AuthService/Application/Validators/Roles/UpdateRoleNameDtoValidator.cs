using Application.DTOs.Roles;
using FluentValidation;

namespace Application.Validators.Roles
{
    public class UpdateRoleNameDtoValidator : AbstractValidator<UpdateRoleNameDto>
    {
        public UpdateRoleNameDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя роли не может быть пустым.")
                .Length(3, 100).WithMessage("Имя роли должно содержать от 3 до 100 символов.");
        }
    }
}
