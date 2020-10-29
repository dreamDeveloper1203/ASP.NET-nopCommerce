﻿using FluentValidation;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Models.Templates;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.Templates
{
    public partial class ManufacturerTemplateValidator : BaseNopValidator<ManufacturerTemplateModel>
    {
        public ManufacturerTemplateValidator(ILocalizationService localizationService, INopDataProvider dataProvider)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.System.Templates.Manufacturer.Name.Required").Result);
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.System.Templates.Manufacturer.ViewPath.Required").Result);

            SetDatabaseValidationRules<ManufacturerTemplate>(dataProvider);
        }
    }
}