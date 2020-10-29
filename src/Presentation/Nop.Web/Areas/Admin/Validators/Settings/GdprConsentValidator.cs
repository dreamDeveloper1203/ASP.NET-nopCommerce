﻿using FluentValidation;
using Nop.Core.Domain.Gdpr;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.Settings
{
    public partial class GdprConsentValidator : BaseNopValidator<GdprConsentModel>
    {
        public GdprConsentValidator(ILocalizationService localizationService, INopDataProvider dataProvider)
        {
            RuleFor(x => x.Message).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.Configuration.Settings.Gdpr.Consent.Message.Required").Result);
            RuleFor(x => x.RequiredMessage)
                .NotEmpty()
                .WithMessage(localizationService.GetResourceAsync("Admin.Configuration.Settings.Gdpr.Consent.RequiredMessage.Required").Result)
                .When(x => x.IsRequired);

            SetDatabaseValidationRules<GdprConsent>(dataProvider);
        }
    }
}