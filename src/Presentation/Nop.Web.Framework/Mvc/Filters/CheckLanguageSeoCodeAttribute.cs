﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Mvc.Filters
{
    /// <summary>
    /// Represents filter attribute that checks SEO friendly URLs for multiple languages and properly redirect if necessary
    /// </summary>
    public sealed class CheckLanguageSeoCodeAttribute : TypeFilterAttribute
    {
        #region Ctor

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public CheckLanguageSeoCodeAttribute() : base(typeof(CheckLanguageSeoCodeFilter))
        {
        }

        #endregion

        #region Nested filter

        /// <summary>
        /// Represents a filter that checks SEO friendly URLs for multiple languages and properly redirect if necessary
        /// </summary>
        private class CheckLanguageSeoCodeFilter : IAsyncActionFilter
        {
            #region Fields

            private readonly IWebHelper _webHelper;
            private readonly IWorkContext _workContext;
            private readonly LocalizationSettings _localizationSettings;

            #endregion

            #region Ctor

            public CheckLanguageSeoCodeFilter(IWebHelper webHelper,
                IWorkContext workContext,
                LocalizationSettings localizationSettings)
            {
                _webHelper = webHelper;
                _workContext = workContext;
                _localizationSettings = localizationSettings;
            }

            #endregion

            #region Utilities

            /// <summary>
            /// Called asynchronously before the action, after model binding is complete.
            /// </summary>
            /// <param name="context">A context for action filters</param>
            /// <returns>A task that on completion indicates the necessary filter actions have been executed</returns>
            private async Task CheckLanguageSeoCodeAsync(ActionExecutingContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                if (context.HttpContext.Request == null)
                    return;

                //only in GET requests
                if (!context.HttpContext.Request.Method.Equals(WebRequestMethods.Http.Get, StringComparison.InvariantCultureIgnoreCase))
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled)
                    return;

                //whether SEO friendly URLs are enabled
                if (!_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                    return;

                //ensure that this route is registered and localizable (LocalizedRoute in RouteProvider)
                if (context.RouteData.Values["language"] == null)
                    return;

                //check whether current page URL is already localized URL
                var pageUrl = await _webHelper.GetRawUrlAsync(context.HttpContext.Request);
                var result = await pageUrl.IsLocalizedUrlAsync(context.HttpContext.Request.PathBase, true);
                if (result.IsLocalized)
                    return;

                //not localized yet, so redirect to the page with working language SEO code
                var language = await _workContext.GetWorkingLanguageAsync();
                pageUrl = pageUrl.AddLanguageSeoCodeToUrl(context.HttpContext.Request.PathBase, true, language);

                context.Result = new LocalRedirectResult(pageUrl, false);
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called asynchronously before the action, after model binding is complete.
            /// </summary>
            /// <param name="context">A context for action filters</param>
            /// <param name="next">A delegate invoked to execute the next action filter or the action itself</param>
            /// <returns>A task that on completion indicates the filter has executed</returns>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                await CheckLanguageSeoCodeAsync(context);
                if (context.Result == null)
                    await next();
            }

            #endregion
        }

        #endregion
    }
}