﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Review type service implementation
    /// </summary>
    public partial class ReviewTypeService : IReviewTypeService
    {
        #region Fields

        private readonly IRepository<ReviewType> _reviewTypeRepository;
        private readonly IRepository<ProductReviewReviewTypeMapping> _productReviewReviewTypeMappingRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStaticCacheManager _cacheManager;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="reviewTypeRepository">Review type repository</param>
        /// <param name="productReviewReviewTypeMappingRepository">Product review and review type mapping repository</param>
        /// <param name="eventPublisher">Event published</param>
        public ReviewTypeService(IStaticCacheManager cacheManager,
            IRepository<ReviewType> reviewTypeRepository,
            IRepository<ProductReviewReviewTypeMapping> productReviewReviewTypeMappingRepository,
            IEventPublisher eventPublisher)
        {
            this._cacheManager = cacheManager;
            this._reviewTypeRepository = reviewTypeRepository;
            this._productReviewReviewTypeMappingRepository = productReviewReviewTypeMappingRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        #region Review type

        /// <summary>
        /// Gets all review types
        /// </summary>
        /// <returns>Review types</returns>
        public virtual IList<ReviewType> GetAllReviewTypes()
        {
            return _cacheManager.Get(NopCatalogDefaults.ReviewTypeAllKey, () =>
            {
                return _reviewTypeRepository.Table
                    .OrderBy(reviewType => reviewType.DisplayOrder).ThenBy(reviewType => reviewType.Id)
                    .ToList();
            });
        }

        /// <summary>
        /// Gets a revie type 
        /// </summary>
        /// <param name="reviewTypeId">Review type identifier</param>
        /// <returns>Review type</returns>
        public virtual ReviewType GetReviewTypeById(int reviewTypeId)
        {
            if (reviewTypeId == 0)
                return null;

            var key = string.Format(NopCatalogDefaults.ReviewTypeByIdKey, reviewTypeId);
            return _cacheManager.Get(key, () => _reviewTypeRepository.GetById(reviewTypeId));
        }

        /// <summary>
        /// Inserts a review type
        /// </summary>
        /// <param name="reviewType">Review type</param>
        public virtual void InsertReviewType(ReviewType reviewType)
        {
            if (reviewType == null)
                throw new ArgumentNullException(nameof(reviewType));

            _reviewTypeRepository.Insert(reviewType);
            _cacheManager.RemoveByPattern(NopCatalogDefaults.ReviewTypeByPatternKey);

            //event notification
            _eventPublisher.EntityInserted(reviewType);
        }

        /// <summary>
        /// Updates a review type
        /// </summary>
        /// <param name="reviewType">Review type</param>
        public virtual void UpdateReviewType(ReviewType reviewType)
        {
            if (reviewType == null)
                throw new ArgumentNullException(nameof(reviewType));

            _reviewTypeRepository.Update(reviewType);
            _cacheManager.RemoveByPattern(NopCatalogDefaults.ReviewTypeByPatternKey);

            //event notification
            _eventPublisher.EntityUpdated(reviewType);
        }

        /// <summary>
        /// Delete review type
        /// </summary>
        /// <param name="reviewType">Review type</param>
        public virtual void DeleteReiewType(ReviewType reviewType)
        {
            if (reviewType == null)
                throw new ArgumentNullException(nameof(reviewType));

            _reviewTypeRepository.Delete(reviewType);
            _cacheManager.RemoveByPattern(NopCatalogDefaults.ReviewTypeByPatternKey);

            //event notification
            _eventPublisher.EntityDeleted(reviewType);
        }

        #endregion

        #region Product review review type mapping

        /// <summary>
        /// Gets product review and review type mappings by product review identifier
        /// </summary>
        /// <param name="productReviewId">The product review identifier</param>
        /// <returns>Product review and review type mapping collection</returns>
        public IList<ProductReviewReviewTypeMapping> GetProductReviewReviewTypeMappingsByProductReviewId(int productReviewId)
        {
            var key = string.Format(NopCatalogDefaults.ProductReviewReviewTypeMappingAllKey, productReviewId);

            return _cacheManager.Get(key, () =>
            {
                var query = from pam in _productReviewReviewTypeMappingRepository.Table
                            orderby pam.Id
                            where pam.ProductReviewId == productReviewId
                            select pam;
                var productReviewReviewTypeMappings = query.ToList();
                return productReviewReviewTypeMappings;
            });
        }

        #endregion

        #endregion
    }
}
