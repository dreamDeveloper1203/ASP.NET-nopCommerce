﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class ProductReviewController : BaseAdminController
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IProductReviewModelFactory _productReviewModelFactory;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;

        #endregion Fields

        #region Ctor

        public ProductReviewController(CatalogSettings catalogSettings,
            ICustomerActivityService customerActivityService,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            IProductReviewModelFactory productReviewModelFactory,
            IProductService productService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService)
        {
            _catalogSettings = catalogSettings;
            _customerActivityService = customerActivityService;
            _eventPublisher = eventPublisher;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _productReviewModelFactory = productReviewModelFactory;
            _productService = productService;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
        }

        #endregion

        #region Methods

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //prepare model
            var model = await _productReviewModelFactory.PrepareProductReviewSearchModel(new ProductReviewSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> List(ProductReviewSearchModel searchModel)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedDataTablesJson();

            //prepare model
            var model = await _productReviewModelFactory.PrepareProductReviewListModel(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> Edit(int id)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //try to get a product review with the specified id
            var productReview = await _productService.GetProductReviewById(id);
            if (productReview == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendor() != null && (await _productService.GetProductById(productReview.ProductId)).VendorId != (await _workContext.GetCurrentVendor()).Id)
                return RedirectToAction("List");

            //prepare model
            var model = await _productReviewModelFactory.PrepareProductReviewModel(null, productReview);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Edit(ProductReviewModel model, bool continueEditing)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //try to get a product review with the specified id
            var productReview = await _productService.GetProductReviewById(model.Id);
            if (productReview == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendor() != null && (await _productService.GetProductById(productReview.ProductId)).VendorId != (await _workContext.GetCurrentVendor()).Id)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                var previousIsApproved = productReview.IsApproved;

                //vendor can edit "Reply text" only
                var isLoggedInAsVendor = await _workContext.GetCurrentVendor() != null;
                if (!isLoggedInAsVendor)
                {
                    productReview.Title = model.Title;
                    productReview.ReviewText = model.ReviewText;
                    productReview.IsApproved = model.IsApproved;
                }

                productReview.ReplyText = model.ReplyText;

                //notify customer about reply
                if (productReview.IsApproved && !string.IsNullOrEmpty(productReview.ReplyText)
                    && _catalogSettings.NotifyCustomerAboutProductReviewReply && !productReview.CustomerNotifiedOfReply)
                {
                    var customerLanguageId = await _genericAttributeService.GetAttribute<Customer, int>(productReview.CustomerId,
                        NopCustomerDefaults.LanguageIdAttribute, productReview.StoreId);

                    var queuedEmailIds = await _workflowMessageService.SendProductReviewReplyCustomerNotificationMessage(productReview, customerLanguageId);
                    if (queuedEmailIds.Any())
                        productReview.CustomerNotifiedOfReply = true;
                }

                await _productService.UpdateProductReview(productReview);

                //activity log
                await _customerActivityService.InsertActivity("EditProductReview",
                   string.Format(await _localizationService.GetResource("ActivityLog.EditProductReview"), productReview.Id), productReview);

                //vendor can edit "Reply text" only
                if (!isLoggedInAsVendor)
                {
                    var product = await _productService.GetProductById(productReview.ProductId);
                    //update product totals
                    await _productService.UpdateProductReviewTotals(product);

                    //raise event (only if it wasn't approved before and is approved now)
                    if (!previousIsApproved && productReview.IsApproved)
                        await _eventPublisher.Publish(new ProductReviewApprovedEvent(productReview));
                }

                _notificationService.SuccessNotification(await _localizationService.GetResource("Admin.Catalog.ProductReviews.Updated"));

                return continueEditing ? RedirectToAction("Edit", new { id = productReview.Id }) : RedirectToAction("List");
            }

            //prepare model
            model = await _productReviewModelFactory.PrepareProductReviewModel(model, productReview, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //try to get a product review with the specified id
            var productReview = await _productService.GetProductReviewById(id);
            if (productReview == null)
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (await _workContext.GetCurrentVendor() != null)
                return RedirectToAction("List");

            await _productService.DeleteProductReview(productReview);

            //activity log
            await _customerActivityService.InsertActivity("DeleteProductReview",
                string.Format(await _localizationService.GetResource("ActivityLog.DeleteProductReview"), productReview.Id), productReview);

            var product = await _productService.GetProductById(productReview.ProductId);

            //update product totals
            await _productService.UpdateProductReviewTotals(product);

            _notificationService.SuccessNotification(await _localizationService.GetResource("Admin.Catalog.ProductReviews.Deleted"));

            return RedirectToAction("List");
        }

        [HttpPost]
        public virtual async Task<IActionResult> ApproveSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //a vendor does not have access to this functionality
            if (await _workContext.GetCurrentVendor() != null)
                return RedirectToAction("List");

            if (selectedIds == null)
                return Json(new { Result = true });

            //filter not approved reviews
            var productReviews = (await _productService.GetProductReviewsByIds(selectedIds.ToArray())).Where(review => !review.IsApproved);

            foreach (var productReview in productReviews)
            {
                productReview.IsApproved = true;
                await _productService.UpdateProductReview(productReview);

                var product = await _productService.GetProductById(productReview.ProductId);

                //update product totals
                await _productService.UpdateProductReviewTotals(product);

                //raise event 
                await _eventPublisher.Publish(new ProductReviewApprovedEvent(productReview));
            }

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> DisapproveSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //a vendor does not have access to this functionality
            if (await _workContext.GetCurrentVendor() != null)
                return RedirectToAction("List");

            if (selectedIds == null)
                return Json(new { Result = true });

            //filter approved reviews
            var productReviews = (await _productService.GetProductReviewsByIds(selectedIds.ToArray())).Where(review => review.IsApproved);

            foreach (var productReview in productReviews)
            {
                productReview.IsApproved = false;
                await _productService.UpdateProductReview(productReview);

                var product = await _productService.GetProductById(productReview.ProductId);

                //update product totals
                await _productService.UpdateProductReviewTotals(product);
            }

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            //a vendor does not have access to this functionality
            if (await _workContext.GetCurrentVendor() != null)
                return RedirectToAction("List");

            if (selectedIds == null)
                return Json(new { Result = true });

            var productReviews = await _productService.GetProductReviewsByIds(selectedIds.ToArray());
            var products = await _productService.GetProductsByIds(productReviews.Select(p => p.ProductId).Distinct().ToArray());

            await _productService.DeleteProductReviews(productReviews);

            //update product totals
            foreach (var product in products)
            {
                await _productService.UpdateProductReviewTotals(product);
            }

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProductReviewReviewTypeMappingList(ProductReviewReviewTypeMappingSearchModel searchModel)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedDataTablesJson();
            var productReview = await _productService.GetProductReviewById(searchModel.ProductReviewId)
                ?? throw new ArgumentException("No product review found with the specified id");

            //prepare model
            var model = await _productReviewModelFactory.PrepareProductReviewReviewTypeMappingListModel(searchModel, productReview);

            return Json(model);
        }

        #endregion
    }
}