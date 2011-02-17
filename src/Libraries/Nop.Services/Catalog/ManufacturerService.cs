
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Manufacturer service
    /// </summary>
    public partial class ManufacturerService : IManufacturerService
    {
        #region Constants
        private const string MANUFACTURERS_ALL_KEY = "Nop.manufacturer.all-{0}";
        private const string MANUFACTURERS_BY_ID_KEY = "Nop.manufacturer.id-{0}";
        private const string PRODUCTMANUFACTURERS_ALLBYMANUFACTURERID_KEY = "Nop.productmanufacturer.allbymanufacturerid-{0}-{1}";
        private const string PRODUCTMANUFACTURERS_ALLBYPRODUCTID_KEY = "Nop.productmanufacturer.allbyproductid-{0}-{1}";
        private const string PRODUCTMANUFACTURERS_BY_ID_KEY = "Nop.productmanufacturer.id-{0}";
        private const string MANUFACTURERS_PATTERN_KEY = "Nop.manufacturer.";
        private const string PRODUCTMANUFACTURERS_PATTERN_KEY = "Nop.productmanufacturer.";
        #endregion

        #region Fields

        private readonly IRepository<Manufacturer> _manufacturerRepository;
        private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheManager _cacheManager;
        #endregion

        #region Ctor
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="manufacturerRepository">Category repository</param>
        /// <param name="productManufacturerRepository">ProductCategory repository</param>
        /// <param name="productRepository">Product repository</param>
        public ManufacturerService(ICacheManager cacheManager,
            IRepository<Manufacturer> manufacturerRepository,
            IRepository<ProductManufacturer> productManufacturerRepository,
            IRepository<Product> productRepository)
        {
            this._cacheManager = cacheManager;
            this._manufacturerRepository = manufacturerRepository;
            this._productManufacturerRepository = productManufacturerRepository;
            this._productRepository = productRepository;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Deletes a manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public void DeleteManufacturer(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                return;

            manufacturer.Deleted = true;
            UpdateManufacturer(manufacturer);
        }

        /// <summary>
        /// Gets all manufacturers
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Manufacturer collection</returns>
        public IList<Manufacturer> GetAllManufacturers(bool showHidden = false)
        {
            string key = string.Format(MANUFACTURERS_ALL_KEY, showHidden);
            return _cacheManager.Get(key, () =>
            {
                var query = from m in _manufacturerRepository.Table
                            orderby m.DisplayOrder
                            where (showHidden || m.Published) &&
                            !m.Deleted
                            select m;
                var manufacturers = query.ToList();
                return manufacturers;
            });
        }

        /// <summary>
        /// Gets a manufacturer
        /// </summary>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <returns>Manufacturer</returns>
        public Manufacturer GetManufacturerById(int manufacturerId)
        {
            if (manufacturerId == 0)
                return null;

            string key = string.Format(MANUFACTURERS_BY_ID_KEY, manufacturerId);
            return _cacheManager.Get(key, () =>
            {
                var manufacturer = _manufacturerRepository.GetById(manufacturerId);
                return manufacturer;
            });
        }

        /// <summary>
        /// Inserts a manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public void InsertManufacturer(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                throw new ArgumentNullException("manufacturer");

            _manufacturerRepository.Insert(manufacturer);

            //cache
            _cacheManager.RemoveByPattern(MANUFACTURERS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTMANUFACTURERS_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        public void UpdateManufacturer(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                throw new ArgumentNullException("manufacturer");

            _manufacturerRepository.Update(manufacturer);

            //cache
            _cacheManager.RemoveByPattern(MANUFACTURERS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTMANUFACTURERS_PATTERN_KEY);
        }

        /// <summary>
        /// Deletes a product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public void DeleteProductManufacturer(ProductManufacturer productManufacturer)
        {
            if (productManufacturer == null)
                return;

            _productManufacturerRepository.Delete(productManufacturer);

            //cache
            _cacheManager.RemoveByPattern(MANUFACTURERS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTMANUFACTURERS_PATTERN_KEY);
        }

        /// <summary>
        /// Gets product manufacturer collection
        /// </summary>
        /// <param name="manufacturerId">Manufacturer identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product manufacturer collection</returns>
        public IList<ProductManufacturer> GetProductManufacturersByManufacturerId(int manufacturerId, bool showHidden = false)
        {
            if (manufacturerId == 0)
                return new List<ProductManufacturer>();

            string key = string.Format(PRODUCTMANUFACTURERS_ALLBYMANUFACTURERID_KEY, showHidden, manufacturerId);
            return _cacheManager.Get(key, () =>
            {
                var query = from pm in _productManufacturerRepository.Table
                            join p in _productRepository.Table on pm.ProductId equals p.Id
                            where pm.ManufacturerId == manufacturerId &&
                                  !p.Deleted &&
                                  (showHidden || p.Published)
                            orderby pm.DisplayOrder
                            select pm;
                var productManufacturers = query.ToList();
                return productManufacturers;
            });
        }

        /// <summary>
        /// Gets a product manufacturer mapping collection
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product manufacturer mapping collection</returns>
        public IList<ProductManufacturer> GetProductManufacturersByProductId(int productId, bool showHidden = false)
        {
            if (productId == 0)
                return new List<ProductManufacturer>();

            string key = string.Format(PRODUCTMANUFACTURERS_ALLBYPRODUCTID_KEY, showHidden, productId);
            return _cacheManager.Get(key, () =>
                                              {
                                                  var query = from pm in _productManufacturerRepository.Table
                                                              join m in _manufacturerRepository.Table on
                                                                  pm.ManufacturerId equals m.Id
                                                              where pm.ProductId == productId &&
                                                                    !m.Deleted &&
                                                                    (showHidden || m.Published)
                                                              orderby pm.DisplayOrder
                                                              select pm;
                                                  var productManufacturers = query.ToList();
                                                  return productManufacturers;
                                              });
        }

        /// <summary>
        /// Gets a product manufacturer mapping 
        /// </summary>
        /// <param name="productManufacturerId">Product manufacturer mapping identifier</param>
        /// <returns>Product manufacturer mapping</returns>
        public ProductManufacturer GetProductManufacturerById(int productManufacturerId)
        {
            if (productManufacturerId == 0)
                return null;

            string key = string.Format(PRODUCTMANUFACTURERS_BY_ID_KEY, productManufacturerId);
            return _cacheManager.Get(key, () =>
            {
                return _productManufacturerRepository.GetById(productManufacturerId);
            });
        }

        /// <summary>
        /// Inserts a product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public void InsertProductManufacturer(ProductManufacturer productManufacturer)
        {
            if (productManufacturer == null)
                throw new ArgumentNullException("productManufacturer");

            _productManufacturerRepository.Insert(productManufacturer);

            //cache
            _cacheManager.RemoveByPattern(MANUFACTURERS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTMANUFACTURERS_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the product manufacturer mapping
        /// </summary>
        /// <param name="productManufacturer">Product manufacturer mapping</param>
        public void UpdateProductManufacturer(ProductManufacturer productManufacturer)
        {
            if (productManufacturer == null)
                throw new ArgumentNullException("productManufacturer");

            _productManufacturerRepository.Update(productManufacturer);

            //cache
            _cacheManager.RemoveByPattern(MANUFACTURERS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTMANUFACTURERS_PATTERN_KEY);
        }

        #endregion
    }
}
