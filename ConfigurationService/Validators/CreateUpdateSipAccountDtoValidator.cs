using ConfigurationService.Models;
using FluentValidation;

namespace ConfigurationService.Validators;

/// <summary>
/// Валидатор для DTO создания/обновления SIP аккаунта
/// </summary>
public class CreateUpdateSipAccountDtoValidator : AbstractValidator<CreateUpdateSipAccountDto>
{
    /// <summary>
    /// Конструктор валидатора
    /// </summary>
    public CreateUpdateSipAccountDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID пользователя обязателен")
            .MaximumLength(256).WithMessage("ID пользователя не может быть длиннее 256 символов");

        RuleFor(x => x.SipAccountName)
            .NotEmpty().WithMessage("SIP username обязателен")
            .MaximumLength(128).WithMessage("SIP username не может быть длиннее 128 символов")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("SIP username может содержать только буквы, цифры, _, -");

        RuleFor(x => x.SipPassword)
            .NotEmpty().WithMessage("SIP пароль обязателен")
            .MinimumLength(6).WithMessage("SIP пароль должен быть не менее 6 символов")
            .MaximumLength(256).WithMessage("SIP пароль не может быть длиннее 256 символов");

        RuleFor(x => x.SipDomain)
            .NotEmpty().WithMessage("SIP домен обязателен")
            .MaximumLength(256).WithMessage("SIP домен не может быть длиннее 256 символов");

        RuleFor(x => x.ProxyUri)
            .NotEmpty().WithMessage("Proxy URI обязателен")
            .MaximumLength(512).WithMessage("Proxy URI не может быть длиннее 512 символов")
            .Must(uri => uri.StartsWith("sip:") || uri.StartsWith("sips:"))
                .WithMessage("Proxy URI должен начинаться с sip: или sips:");

        RuleFor(x => x.ProxyTransport)
            .NotEmpty().WithMessage("Транспорт обязателен")
            .Must(t => t == "UDP" || t == "TCP" || t == "TLS")
                .WithMessage("Транспорт должен быть UDP, TCP или TLS");

        RuleFor(x => x.RegisterTtl)
            .GreaterThan(0).WithMessage("TTL должен быть больше 0")
            .LessThanOrEqualTo(86400).WithMessage("TTL не должен превышать 86400 секунд (24 часа)");
    }
}
