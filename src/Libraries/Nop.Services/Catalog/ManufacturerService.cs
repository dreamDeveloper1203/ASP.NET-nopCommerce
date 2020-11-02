﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Discounts;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Manufacturer service
    /// </summary>
    public partial class ManufacturerService : IManufacturerService
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICustomerService _customerService;
        private readonly IRepository<AclRecord> _aclRepository;
        private readonly IRepository<DiscountManufacturerMapping> _discountManufacturerMappingRepository;
        private readonly IRepository<Manufacturer> _manufacturerRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
        private readonly IRepository<StoreMapping> _storeMappingRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public ManufacturerService(CatalogSettings catalogSettings,
            ICustomerService customerService,
            IRepository<AclRecord> aclRepository,
            IRepository<DiscountManufacturerMapping> discountManufacturerMappingRepository,
            IRepository<Manufacturer> manufacturerRepository,
            IRepository<Product> productRepository,
            IRepository<ProductManufacturer> productManufacturerRepository,
            IRepository<StoreMapping> storeMappingRepository,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _catalogSettings = catalogSettings;
            _customerService = customerService;
            _aclRepository = aclRepository;
            _discountManufacturerMappingRepository = discountManufacturerMappingRepository;
            _manufacturerRepository = manufacturerRepository;
            _productRepository = productRepository;
            _productManufacturerRepository = productManufacturerRepository;
            _storeMappingRepository = storeMappingRepository;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clean up manufacturer references for a specified discount
        /// </summary>
        /// <param name="discount">Discount</param>
        public virtual async Task ClearDiscountManufacturerMappingAsync(Discount discount)
        {
            if (discount is null)
                throw new ArgumentNullException(nameof(discount));

            var mappings = _discountManufacturerMappingRepository.Table.Where(dcm => dcm.DiscountId == discount.Id);

            await _discountManufacturerMappingRepository.DeleteAsync(mappings.ToList());
        }

        /// <summary>
        /// Deletes a manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public virtual async Task DeleteManufacturerAsync(Manufacturer manufacturer)
        {
            await _manufacturerRepository.DeleteAsync(manufacturer);
        }

        /// <summary>
        /// Delete manufacturers
        /// </summary>
        /// <param name="manufacturers">Manufacturers</param>
        public virtual async Task DeleteManufacturersAsync(IList<Manufacturer> manufacturers)
        {
            await _manufacturerRepository.DeleteAsync(manufacturers);
        }

        /// <summary>
        /// Gets all manufacturers
        /// </summary>
        /// <param name="manufacturerName">Manufacturer name</param>
        /// <param name="storeId">Store identifier; 0 if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="overridePublished">
        /// null - process "Published" property according to "showHidden" parameter
        /// true - load only "Published" products
        /// false - load only "Unpublished" products
        /// </param>
        /// <returns>Manufacturers</returns>
        public virtual async Task<IPagedList<Manufacturer>> GetAllManufacturersAsync(string manufacturerName = "",
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false,
            bool? overridePublished = null)
        {
            return await _manufacturerRepository.GetAllPagedAsync(query =>
            {
                if (!showHidden)
                    query = query.Where(m => m.Published);
                if (!string.IsNullOrWhiteSpace(manufacturerName))
                    query = query.Where(m => m.Name.Contains(manufacturerName));
                query = query.Where(m => !m.Deleted);
                if (overridePublished.HasValue)
                    query = query.Where(m => m.Published == overridePublished.Value);

                query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);

                if ((storeId <= 0 || _catalogSettings.IgnoreStoreLimitations) &&
                    (showHidden || _catalogSettings.IgnoreAcl))
                    return query;

                if (!showHidden && !_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = _customerService.GetCustomerRoleIdsAsync(_workContext.GetCurrentCustomerAsync().Result).Result;
                    query = from m in query
                        join acl in _aclRepository.Table
                            on new {c1 = m.Id, c2 = nameof(Manufacturer)} equals new
                            {
                                c1 = acl.EntityId, c2 = acl.EntityName
                            } into m_acl
                        from acl in m_acl.DefaultIfEmpty()
                        where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                        select m;
                }

                if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
                {
                    //store mapping
                    query = from m in query
                        join sm in _storeMappingRepository.Table
                            on new {c1 = m.Id, c2 = nameof(Manufacturer)} equals new
                            {
                                c1 = sm.EntityId, c2 = sm.EntityName
                            } into m_sm
                        from sm in m_sm.DefaultIfEmpty()
                        where !m.LimitedToStores || storeId == sm.StoreId
                        select m;
                }

                return query.Distinct();
            }, pageIndex, pageSize);
        }

        /// <summary>
        /// Get manufacturer identifiers to which a discount is applied
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <returns>Manufacturer identifiers</returns>
        public virtual async Task<IList<int>> GetAppliedManufacturerIdsAsync(Discount discount, Customer customer)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(NopDiscountDefaults.ManufacturerIdsByDiscountCacheKey, 
                discount,
                await _customerService.GetCustomerRoleIdsAsync(customer),
                await _storeContext.GetCurrentStoreAsync());

            var query = _discountManufacturerMappingRepository.Table.Where(dmm => dmm.DiscountId == discount.Id)
                .Select(dmm => dmm.EntityId);

            var result = await _staticCacheManager.GetAsync(cacheKey, async () => await query.ToAsyncEnumerable().ToListAsync());

            return result;
        }

        /// <summary>
        /// Gets a manufacturer
        /// </summary>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <returns>Manufacturer</returns>
        public virtual async Task<Manufacturer> GetManufacturerByIdAsync(int manufacturerId)
        {
            return await _manufacturerRepository.GetByIdAsync(manufacturerId, cache => default);
        }

        /// <summary>
        /// Get manufacturers for which a discount is applied
        /// </summary>
        /// <param name="discountId">Discount identifier; pass null to load all records</param>
        /// <param name="showHidden">A value indicating whether to load deleted manufacturers</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of manufacturers</returns>
        public virtual async Task<IPagedList<Manufacturer>> GetManufacturersWithAppliedDiscountAsync(int? discountId = null,
            bool showHidden = false, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var manufacturers = _manufacturerRepository.Table;

            if (discountId.HasValue)
                manufacturers = from manufacturer in manufacturers
                    join dmm in _discountManufacturerMappingRepository.Table on manufacturer.Id equals dmm.EntityId
                    where dmm.DiscountId == discountId.Value
                    select manufacturer;

            if (!showHidden)
                manufacturers = manufacturers.Where(manufacturer => !manufacturer.Deleted);

            manufacturers = manufacturers.OrderBy(manufacturer => manufacturer.DisplayOrder).ThenBy(manufacturer => manufacturer.Id);

            return await manufacturers.ToPagedListAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Gets manufacturers by identifier
        /// </summary>
        /// <param name="manufacturerIds">manufacturer identifiers</param>
        /// <returns>Manufacturers</returns>
        public virtual async Task<IList<Manufacturer>> GetManufacturersByIdsAsync(int[] manufacturerIds)
        {
            return await _manufacturerRepository.GetByIdsAsync(manufacturerIds);
        }

        /// <summary>
        /// Inserts a manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public virtual async Task InsertManufacturerAsync(Manufacturer manufacturer)
        {
            await _manufacturerRepository.InsertAsync(manufacturer);
        }

        /// <summary>
        /// Updates the manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public virtual async Task UpdateManufacturerAsync(Manufacturer manufacturer)
        {
            await _manufacturerRepository.UpdateAsync(manufacturer);
        }

        /// <summary>
        /// Deletes a product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public virtual async Task DeleteProductManufacturerAsync(ProductManufacturer productManufacturer)
        {
            await _productManufacturerRepository.DeleteAsync(productManufacturer);
        }

        /// <summary>
        /// Gets product manufacturer collection
        /// </summary>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product manufacturer collection</returns>
        public virtual async Task<IPagedList<ProductManufacturer>> GetProductManufacturersByManufacturerIdAsync(int manufacturerId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            if (manufacturerId == 0)
                return new PagedList<ProductManufacturer>(new List<ProductManufacturer>(), pageIndex, pageSize);

            var query = from pm in _productManufacturerRepository.Table
                join p in _productRepository.Table on pm.ProductId equals p.Id
                where pm.ManufacturerId == manufacturerId &&
                      !p.Deleted &&
                      (showHidden || p.Published)
                orderby pm.DisplayOrder, pm.Id
                select pm;

            if (!showHidden && (!_catalogSettings.IgnoreAcl || !_catalogSettings.IgnoreStoreLimitations))
            {
                if (!_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = await _customerService.GetCustomerRoleIdsAsync(await _workContext.GetCurrentCustomerAsync());
                    query = from pm in query
                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                        join acl in _aclRepository.Table
                            on new
                            {
                                c1 = m.Id,
                                c2 = nameof(Manufacturer)
                            } 
                            equals new
                            {
                                c1 = acl.EntityId,
                                c2 = acl.EntityName
                            } 
                            into m_acl
                        from acl in m_acl.DefaultIfEmpty()
                        where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                        select pm;
                }

                if (!_catalogSettings.IgnoreStoreLimitations)
                {
                    //store mapping
                    var currentStoreId = (await _storeContext.GetCurrentStoreAsync()).Id;
                    query = from pm in query
                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                        join sm in _storeMappingRepository.Table
                            on new
                            {
                                c1 = m.Id,
                                c2 = nameof(Manufacturer)
                            } 
                            equals new
                            {
                                c1 = sm.EntityId,
                                c2 = sm.EntityName
                            } 
                            into m_sm
                        from sm in m_sm.DefaultIfEmpty()
                        where !m.LimitedToStores || currentStoreId == sm.StoreId
                        select pm;
                }

                query = query.Distinct();
            }

            var productManufacturers = await query.ToPagedListAsync(pageIndex, pageSize);

            return productManufacturers;
        }

        /// <summary>
        /// Gets a product manufacturer mapping collection
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product manufacturer mapping collection</returns>
        public virtual async Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdAsync(int productId,
            bool showHidden = false)
        {
            if (productId == 0)
                return new List<ProductManufacturer>();

            var key = _staticCacheManager.PrepareKeyForDefaultCache(NopCatalogDefaults.ProductManufacturersByProductCacheKey, productId,
                showHidden, await _workContext.GetCurrentCustomerAsync(), await _storeContext.GetCurrentStoreAsync());

            var query = from pm in _productManufacturerRepository.Table
                join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                where pm.ProductId == productId &&
                      !m.Deleted &&
                      (showHidden || m.Published)
                orderby pm.DisplayOrder, pm.Id
                select pm;

            if (!showHidden && (!_catalogSettings.IgnoreAcl || !_catalogSettings.IgnoreStoreLimitations))
            {
                if (!_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = await _customerService.GetCustomerRoleIdsAsync(await _workContext.GetCurrentCustomerAsync());
                    query = from pm in query
                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                        join acl in _aclRepository.Table
                            on new
                            {
                                c1 = m.Id,
                                c2 = nameof(Manufacturer)
                            } 
                            equals new
                            {
                                c1 = acl.EntityId,
                                c2 = acl.EntityName
                            } 
                            into m_acl
                        from acl in m_acl.DefaultIfEmpty()
                        where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                        select pm;
                }

                if (!_catalogSettings.IgnoreStoreLimitations)
                {
                    //store mapping
                    var currentStoreId = (await _storeContext.GetCurrentStoreAsync()).Id;
                    query = from pm in query
                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                        join sm in _storeMappingRepository.Table
                            on new
                            {
                                c1 = m.Id,
                                c2 = nameof(Manufacturer)
                            } 
                            equals new
                            {
                                c1 = sm.EntityId,
                                c2 = sm.EntityName
                            } 
                            into m_sm
                        from sm in m_sm.DefaultIfEmpty()
                        where !m.LimitedToStores || currentStoreId == sm.StoreId
                        select pm;
                }

                query = query.Distinct();
            }

            var productManufacturers = await _staticCacheManager.GetAsync(key, async () => await query.ToAsyncEnumerable().ToListAsync());

            return productManufacturers;
        }

        /// <summary>
        /// Gets a product manufacturer mapping 
        /// </summary>
        /// <param name="productManufacturerId">Product manufacturer mapping identifier</param>
        /// <returns>Product manufacturer mapping</returns>
        public virtual async Task<ProductManufacturer> GetProductManufacturerByIdAsync(int productManufacturerId)
        {
            return await _productManufacturerRepository.GetByIdAsync(productManufacturerId, cache => default);
        }

        /// <summary>
        /// Inserts a product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public virtual async Task InsertProductManufacturerAsync(ProductManufacturer productManufacturer)
        {
            await _productManufacturerRepository.InsertAsync(productManufacturer);
        }

        /// <summary>
        /// Updates the product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public virtual async Task UpdateProductManufacturerAsync(ProductManufacturer productManufacturer)
        {
            await _productManufacturerRepository.UpdateAsync(productManufacturer);
        }

        /// <summary>
        /// Get manufacturer IDs for products
        /// </summary>
        /// <param name="productIds">Products IDs</param>
        /// <returns>Manufacturer IDs for products</returns>
        public virtual async Task<IDictionary<int, int[]>> GetProductManufacturerIdsAsync(int[] productIds)
        {
            var query = _productManufacturerRepository.Table;

            return (await query.Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, p.ManufacturerId })
                .ToAsyncEnumerable()
                .ToListAsync())
                .GroupBy(a => a.ProductId)
                .ToDictionary(items => items.Key, items => items.Select(a => a.ManufacturerId).ToArray());
        }

        /// <summary>
        /// Returns a list of names of not existing manufacturers
        /// </summary>
        /// <param name="manufacturerIdsNames">The names and/or IDs of the manufacturers to check</param>
        /// <returns>List of names and/or IDs not existing manufacturers</returns>
        public virtual async Task<string[]> GetNotExistingManufacturersAsync(string[] manufacturerIdsNames)
        {
            if (manufacturerIdsNames == null)
                throw new ArgumentNullException(nameof(manufacturerIdsNames));

            var query = _manufacturerRepository.Table;
            var queryFilter = manufacturerIdsNames.Distinct().ToArray();
            //filtering by name
            var filter = query.Select(m => m.Name).Where(m => queryFilter.Contains(m)).ToList();
            queryFilter = queryFilter.Except(filter).ToArray();

            //if some names not found
            if (!queryFilter.Any())
                return queryFilter.ToArray();

            //filtering by IDs
            filter = await query.Select(c => c.Id.ToString())
                .Where(c => queryFilter.Contains(c))
                .ToAsyncEnumerable()
                .ToListAsync();

            return queryFilter.Except(filter).ToArray();
        }

        /// <summary>
        /// Returns a ProductManufacturer that has the specified values
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <returns>A ProductManufacturer that has the specified values; otherwise null</returns>
        public virtual ProductManufacturer FindProductManufacturerAsync(IList<ProductManufacturer> source, int productId, int manufacturerId)
        {
            foreach (var productManufacturer in source)
                if (productManufacturer.ProductId == productId && productManufacturer.ManufacturerId == manufacturerId)
                    return productManufacturer;

            return null;
        }

        /// <summary>
        /// Get a discount-manufacturer mapping record
        /// </summary>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <param name="discountId">Discount identifier</param>
        /// <returns>Result</returns>
        public async Task<DiscountManufacturerMapping> GetDiscountAppliedToManufacturerAsync(int manufacturerId, int discountId)
        {
            return await _discountManufacturerMappingRepository.Table
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(dcm => dcm.EntityId == manufacturerId && dcm.DiscountId == discountId);
        }

        /// <summary>
        /// Inserts a discount-manufacturer mapping record
        /// </summary>
        /// <param name="discountManufacturerMapping">Discount-manufacturer mapping</param>
        public async Task InsertDiscountManufacturerMappingAsync(DiscountManufacturerMapping discountManufacturerMapping)
        {
            await _discountManufacturerMappingRepository.InsertAsync(discountManufacturerMapping);
        }

        /// <summary>
        /// Deletes a discount-manufacturer mapping record
        /// </summary>
        /// <param name="discountManufacturerMapping">Discount-manufacturer mapping</param>
        public async Task DeleteDiscountManufacturerMappingAsync(DiscountManufacturerMapping discountManufacturerMapping)
        {
            await _discountManufacturerMappingRepository.DeleteAsync(discountManufacturerMapping);
        }

        #endregion
    }
}