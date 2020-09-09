﻿using System.Threading.Tasks;
using Nop.Web.Areas.Admin.Models.Messages;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the newsletter subscription model factory
    /// </summary>
    public partial interface INewsletterSubscriptionModelFactory
    {
        /// <summary>
        /// Prepare newsletter subscription search model
        /// </summary>
        /// <param name="searchModel">Newsletter subscription search model</param>
        /// <returns>Newsletter subscription search model</returns>
        Task<NewsletterSubscriptionSearchModel> PrepareNewsletterSubscriptionSearchModel(NewsletterSubscriptionSearchModel searchModel);

        /// <summary>
        /// Prepare paged newsletter subscription list model
        /// </summary>
        /// <param name="searchModel">Newsletter subscription search model</param>
        /// <returns>Newsletter subscription list model</returns>
        Task<NewsletterSubscriptionListModel> PrepareNewsletterSubscriptionListModel(NewsletterSubscriptionSearchModel searchModel);
    }
}