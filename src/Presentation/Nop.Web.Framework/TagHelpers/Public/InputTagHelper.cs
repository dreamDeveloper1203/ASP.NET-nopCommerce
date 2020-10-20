﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Nop.Web.Framework.TagHelpers.Public
{
    /// <summary>
    /// "input" tag helper
    /// </summary>
    [HtmlTargetElement("input", Attributes = FOR_ATTRIBUTE_NAME)]
    public class InputTagHelper : Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper
    {
        #region Constants

        private const string FOR_ATTRIBUTE_NAME = "asp-for";
        private const string DISABLED_ATTRIBUTE_NAME = "asp-disabled";

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the input is disabled
        /// </summary>
        [HtmlAttributeName(DISABLED_ATTRIBUTE_NAME)]
        public string IsDisabled { set; get; }

        #endregion

        #region Ctor

        public InputTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously executes the tag helper with the given context and output
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag</param>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            //add disabled attribute
            if (bool.TryParse(IsDisabled, out var disabled) && disabled)
                output.Attributes.Add(new TagHelperAttribute("disabled", "disabled"));

            await base.ProcessAsync(context, output);
        }

        #endregion
    }
}