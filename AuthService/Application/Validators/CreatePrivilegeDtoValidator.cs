using Application.DTOs.Privileges;
using FluentValidation;

namespace Application.Validators
{
    public class CreatePrivilegeDtoValidator : AbstractValidator<CreatePrivilegeDto>
    {
        public CreatePrivilegeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название привилегии обязательно")
                .MaximumLength(100);
        }
    }
}
