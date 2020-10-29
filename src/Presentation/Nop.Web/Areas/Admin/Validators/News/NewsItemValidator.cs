﻿using FluentValidation;
using Nop.Core.Domain.News;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Models.News;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.News
{
    public partial class NewsItemValidator : BaseNopValidator<NewsItemModel>
    {
        public NewsItemValidator(ILocalizationService localizationService, INopDataProvider dataProvider)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.ContentManagement.News.NewsItems.Fields.Title.Required").Result);

            RuleFor(x => x.Short).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.ContentManagement.News.NewsItems.Fields.Short.Required").Result);

            RuleFor(x => x.Full).NotEmpty().WithMessage(localizationService.GetResourceAsync("Admin.ContentManagement.News.NewsItems.Fields.Full.Required").Result);

            RuleFor(x => x.SeName).Length(0, NopSeoDefaults.SearchEngineNameLength)
                .WithMessage(string.Format(localizationService.GetResourceAsync("Admin.SEO.SeName.MaxLengthValidation").Result, NopSeoDefaults.SearchEngineNameLength));

            SetDatabaseValidationRules<NewsItem>(dataProvider);
        }
    }
}