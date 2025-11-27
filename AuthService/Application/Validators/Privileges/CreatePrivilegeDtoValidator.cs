using Application.DTOs.Privileges;
using FluentValidation;

namespace Application.Validators.Privileges
{
    public class CreatePrivilegeDtoValidator : AbstractValidator<CreatePrivilegeDto>
    {
        public CreatePrivilegeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название привилегии обязательно")
                .Length(3, 100).WithMessage("Длина названия — от 3 до 100 символов");
        }
    }
}
