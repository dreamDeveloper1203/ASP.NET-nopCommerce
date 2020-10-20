﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;

namespace Nop.Services.Localization
{
    /// <summary>
    /// Represents extensions for localized URLs
    /// </summary>
    public static class LocalizedUrlExtensions
    {
        /// <summary>
        /// Get a value indicating whether URL is localized (contains SEO code)
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="pathBase">Application path base</param>
        /// <param name="isRawPath">A value indicating whether passed URL is raw URL</param>
        /// <returns>True if passed URL contains SEO code; otherwise false. Language whose SEO code is in the URL if URL is localized</returns>
        public static async Task<(bool IsLocalized, Language Language)> IsLocalizedUrl(this string url, PathString pathBase, bool isRawPath)
        {
            if (string.IsNullOrEmpty(url))
                return (false, null);

            //remove application path from raw URL
            if (isRawPath)
                url = url.RemoveApplicationPathFromRawUrl(pathBase);

            //get first segment of passed URL
            var firstSegment = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(firstSegment))
                return (false, null);

            //suppose that the first segment is the language code and try to get language
            var languageService = EngineContext.Current.Resolve<ILanguageService>();
            var language = (await languageService.GetAllLanguages())
                .FirstOrDefault(urlLanguage => urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.InvariantCultureIgnoreCase));

            //if language exists and published passed URL is localized
            return (language?.Published ?? false, language);
        }

        /// <summary>
        /// Remove application path from raw URL
        /// </summary>
        /// <param name="rawUrl">Raw URL</param>
        /// <param name="pathBase">Application path base</param>
        /// <returns>Result</returns>
        public static string RemoveApplicationPathFromRawUrl(this string rawUrl, PathString pathBase)
        {
            new PathString(rawUrl).StartsWithSegments(pathBase, out var result);
            return WebUtility.UrlDecode(result);
        }

        /// <summary>
        /// Remove language SEO code from URL
        /// </summary>
        /// <param name="url">Raw URL</param>
        /// <param name="pathBase">Application path base</param>
        /// <param name="isRawPath">A value indicating whether passed URL is raw URL</param>
        /// <returns>URL without language SEO code</returns>
        public static string RemoveLanguageSeoCodeFromUrl(this string url, PathString pathBase, bool isRawPath)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            //remove application path from raw URL
            if (isRawPath)
                url = url.RemoveApplicationPathFromRawUrl(pathBase);

            //get result URL
            url = url.TrimStart('/');
            var result = url.Contains('/') ? url.Substring(url.IndexOf('/')) : string.Empty;

            //and add back application path to raw URL
            if (isRawPath)
                result = pathBase + result;

            return result;
        }

        /// <summary>
        /// Add language SEO code to URL
        /// </summary>
        /// <param name="url">Raw URL</param>
        /// <param name="pathBase">Application path base</param>
        /// <param name="isRawPath">A value indicating whether passed URL is raw URL</param>
        /// <param name="language">Language</param>
        /// <returns>Result</returns>
        public static string AddLanguageSeoCodeToUrl(this string url, PathString pathBase, bool isRawPath, Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            //null validation is not required
            //if (string.IsNullOrEmpty(url))
            //    return url;

            //remove application path from raw URL
            if (isRawPath && !string.IsNullOrEmpty(url))
                url = url.RemoveApplicationPathFromRawUrl(pathBase);

            //add language code
            url = $"/{language.UniqueSeoCode}{url}";

            //and add back application path to raw URL
            if (isRawPath)
                url = pathBase + url;

            return url;
        }
    }
}