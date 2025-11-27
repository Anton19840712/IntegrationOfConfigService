using Application.DTOs.Requests;
using FluentValidation;

namespace Application.Validators
{
    public class CreateServiceClientRequestValidator : AbstractValidator<CreateServiceClientRequest>
    {
        public CreateServiceClientRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя сервисного клиента обязательно")
                .MaximumLength(100);
        }
    }
}
