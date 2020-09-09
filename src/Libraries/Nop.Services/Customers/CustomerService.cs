﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using LinqToDB;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Caching.Extensions;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Customer service
    /// </summary>
    public partial class CustomerService : ICustomerService
    {
        #region Fields

        private readonly CachingSettings _cachingSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IRepository<Address> _customerAddressRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerAddressMapping> _customerAddressMappingRepository;
        private readonly IRepository<CustomerCustomerRoleMapping> _customerCustomerRoleMappingRepository;
        private readonly IRepository<CustomerPassword> _customerPasswordRepository;
        private readonly IRepository<CustomerRole> _customerRoleRepository;
        private readonly IRepository<GenericAttribute> _gaRepository;
        private readonly IRepository<ShoppingCartItem> _shoppingCartRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public CustomerService(CachingSettings cachingSettings,
            CustomerSettings customerSettings,
            ICacheKeyService cacheKeyService,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            IRepository<Address> customerAddressRepository,
            IRepository<Customer> customerRepository,
            IRepository<CustomerAddressMapping> customerAddressMappingRepository,
            IRepository<CustomerCustomerRoleMapping> customerCustomerRoleMappingRepository,
            IRepository<CustomerPassword> customerPasswordRepository,
            IRepository<CustomerRole> customerRoleRepository,
            IRepository<GenericAttribute> gaRepository,
            IRepository<ShoppingCartItem> shoppingCartRepository,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            ShoppingCartSettings shoppingCartSettings)
        {
            _cachingSettings = cachingSettings;
            _customerSettings = customerSettings;
            _cacheKeyService = cacheKeyService;
            _eventPublisher = eventPublisher;
            _genericAttributeService = genericAttributeService;
            _customerAddressRepository = customerAddressRepository;
            _customerRepository = customerRepository;
            _customerAddressMappingRepository = customerAddressMappingRepository;
            _customerCustomerRoleMappingRepository = customerCustomerRoleMappingRepository;
            _customerPasswordRepository = customerPasswordRepository;
            _customerRoleRepository = customerRoleRepository;
            _gaRepository = gaRepository;
            _shoppingCartRepository = shoppingCartRepository;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        #region Customers

        /// <summary>
        /// Gets all customers
        /// </summary>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="customerRoleIds">A list of customer role identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
        /// <param name="email">Email; null to load all customers</param>
        /// <param name="username">Username; null to load all customers</param>
        /// <param name="firstName">First name; null to load all customers</param>
        /// <param name="lastName">Last name; null to load all customers</param>
        /// <param name="dayOfBirth">Day of birth; 0 to load all customers</param>
        /// <param name="monthOfBirth">Month of birth; 0 to load all customers</param>
        /// <param name="company">Company; null to load all customers</param>
        /// <param name="phone">Phone; null to load all customers</param>
        /// <param name="zipPostalCode">Phone; null to load all customers</param>
        /// <param name="ipAddress">IP address; null to load all customers</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="getOnlyTotalCount">A value in indicating whether you want to load only total number of records. Set to "true" if you don't want to load data from database</param>
        /// <returns>Customers</returns>
        public virtual Task<IPagedList<Customer>> GetAllCustomers(DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int affiliateId = 0, int vendorId = 0, int[] customerRoleIds = null,
            string email = null, string username = null, string firstName = null, string lastName = null,
            int dayOfBirth = 0, int monthOfBirth = 0,
            string company = null, string phone = null, string zipPostalCode = null, string ipAddress = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
        {
            var query = _customerRepository.Table;
            if (createdFromUtc.HasValue)
                query = query.Where(c => createdFromUtc.Value <= c.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(c => createdToUtc.Value >= c.CreatedOnUtc);
            if (affiliateId > 0)
                query = query.Where(c => affiliateId == c.AffiliateId);
            if (vendorId > 0)
                query = query.Where(c => vendorId == c.VendorId);

            query = query.Where(c => !c.Deleted);

            if (customerRoleIds != null && customerRoleIds.Length > 0)
            {
                query = query.Join(_customerCustomerRoleMappingRepository.Table, x => x.Id, y => y.CustomerId,
                        (x, y) => new { Customer = x, Mapping = y })
                    .Where(z => customerRoleIds.Contains(z.Mapping.CustomerRoleId))
                    .Select(z => z.Customer)
                    .Distinct();
            }

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(c => c.Email.Contains(email));
            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(c => c.Username.Contains(username));
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.FirstNameAttribute &&
                                z.Attribute.Value.Contains(firstName))
                    .Select(z => z.Customer);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.LastNameAttribute &&
                                z.Attribute.Value.Contains(lastName))
                    .Select(z => z.Customer);
            }

            //date of birth is stored as a string into database.
            //we also know that date of birth is stored in the following format YYYY-MM-DD (for example, 1983-02-18).
            //so let's search it as a string
            if (dayOfBirth > 0 && monthOfBirth > 0)
            {
                //both are specified
                var dateOfBirthStr = monthOfBirth.ToString("00", CultureInfo.InvariantCulture) + "-" + dayOfBirth.ToString("00", CultureInfo.InvariantCulture);

                //z.Attribute.Value.Length - dateOfBirthStr.Length = 5
                //dateOfBirthStr.Length = 5
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.DateOfBirthAttribute &&
                                z.Attribute.Value.Substring(5, 5) == dateOfBirthStr)
                    .Select(z => z.Customer);
            }
            else if (dayOfBirth > 0)
            {
                //only day is specified
                var dateOfBirthStr = dayOfBirth.ToString("00", CultureInfo.InvariantCulture);

                //z.Attribute.Value.Length - dateOfBirthStr.Length = 8
                //dateOfBirthStr.Length = 2
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.DateOfBirthAttribute &&
                                z.Attribute.Value.Substring(8, 2) == dateOfBirthStr)
                    .Select(z => z.Customer);
            }
            else if (monthOfBirth > 0)
            {
                //only month is specified
                var dateOfBirthStr = "-" + monthOfBirth.ToString("00", CultureInfo.InvariantCulture) + "-";
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.DateOfBirthAttribute &&
                                z.Attribute.Value.Contains(dateOfBirthStr))
                    .Select(z => z.Customer);
            }
            //search by company
            if (!string.IsNullOrWhiteSpace(company))
            {
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.CompanyAttribute &&
                                z.Attribute.Value.Contains(company))
                    .Select(z => z.Customer);
            }
            //search by phone
            if (!string.IsNullOrWhiteSpace(phone))
            {
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.PhoneAttribute &&
                                z.Attribute.Value.Contains(phone))
                    .Select(z => z.Customer);
            }
            //search by zip
            if (!string.IsNullOrWhiteSpace(zipPostalCode))
            {
                query = query
                    .Join(_gaRepository.Table, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
                    .Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
                                z.Attribute.Key == NopCustomerDefaults.ZipPostalCodeAttribute &&
                                z.Attribute.Value.Contains(zipPostalCode))
                    .Select(z => z.Customer);
            }

            //search by IpAddress
            if (!string.IsNullOrWhiteSpace(ipAddress) && CommonHelper.IsValidIpAddress(ipAddress))
            {
                query = query.Where(w => w.LastIpAddress == ipAddress);
            }

            query = query.OrderByDescending(c => c.CreatedOnUtc);

            var customers = new PagedList<Customer>(query, pageIndex, pageSize, getOnlyTotalCount);

            return Task.FromResult((IPagedList<Customer>)customers);
        }

        /// <summary>
        /// Gets online customers
        /// </summary>
        /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
        /// <param name="customerRoleIds">A list of customer role identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Customers</returns>
        public virtual Task<IPagedList<Customer>> GetOnlineCustomers(DateTime lastActivityFromUtc,
            int[] customerRoleIds, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _customerRepository.Table;
            query = query.Where(c => lastActivityFromUtc <= c.LastActivityDateUtc);
            query = query.Where(c => !c.Deleted);

            if (customerRoleIds != null && customerRoleIds.Length > 0)
                query = query.Where(c => _customerCustomerRoleMappingRepository.Table.Any(ccrm => ccrm.CustomerId == c.Id && customerRoleIds.Contains(ccrm.CustomerRoleId)));

            query = query.OrderByDescending(c => c.LastActivityDateUtc);
            var customers = new PagedList<Customer>(query, pageIndex, pageSize);

            return Task.FromResult((IPagedList<Customer>)customers);
        }

        /// <summary>
        /// Gets customers with shopping carts
        /// </summary>
        /// <param name="shoppingCartType">Shopping cart type; pass null to load all records</param>
        /// <param name="storeId">Store identifier; pass 0 to load all records</param>
        /// <param name="productId">Product identifier; pass null to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); pass null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); pass null to load all records</param>
        /// <param name="countryId">Billing country identifier; pass null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Customers</returns>
        public virtual Task<IPagedList<Customer>> GetCustomersWithShoppingCarts(ShoppingCartType? shoppingCartType = null,
            int storeId = 0, int? productId = null,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int? countryId = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            //get all shopping cart items
            var items = _shoppingCartRepository.Table;

            //filter by type
            if (shoppingCartType.HasValue)
                items = items.Where(item => item.ShoppingCartTypeId == (int)shoppingCartType.Value);

            //filter shopping cart items by store
            if (storeId > 0 && !_shoppingCartSettings.CartsSharedBetweenStores)
                items = items.Where(item => item.StoreId == storeId);

            //filter shopping cart items by product
            if (productId > 0)
                items = items.Where(item => item.ProductId == productId);

            //filter shopping cart items by date
            if (createdFromUtc.HasValue)
                items = items.Where(item => createdFromUtc.Value <= item.CreatedOnUtc);
            if (createdToUtc.HasValue)
                items = items.Where(item => createdToUtc.Value >= item.CreatedOnUtc);

            //get all active customers
            var customers = _customerRepository.Table.Where(customer => customer.Active && !customer.Deleted);

            //filter customers by billing country
            if (countryId > 0)
                customers = from c in customers
                    join a in _customerAddressRepository.Table on c.BillingAddressId equals a.Id
                    where a.CountryId == countryId
                    select c;

            var customersWithCarts = from c in customers
                join item in items on c.Id equals item.CustomerId
                orderby c.Id
                select c;

            return Task.FromResult((IPagedList<Customer>)new PagedList<Customer>(customersWithCarts.Distinct(), pageIndex, pageSize));
        }

        /// <summary>
        /// Gets customer for shopping cart
        /// </summary>
        /// <param name="shoppingCart">Shopping cart</param>
        /// <returns>Result</returns>
        public virtual async Task<Customer> GetShoppingCartCustomer(IList<ShoppingCartItem> shoppingCart)
        {
            var customerId = shoppingCart.FirstOrDefault()?.CustomerId;

            return customerId.HasValue && customerId != 0 ? await GetCustomerById(customerId.Value) : null;
        }

        /// <summary>
        /// Delete a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task DeleteCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (customer.IsSystemAccount)
                throw new NopException($"System customer account ({customer.SystemName}) could not be deleted");

            customer.Deleted = true;

            if (_customerSettings.SuffixDeletedCustomers)
            {
                if (!string.IsNullOrEmpty(customer.Email))
                    customer.Email += "-DELETED";
                if (!string.IsNullOrEmpty(customer.Username))
                    customer.Username += "-DELETED";
            }

            await UpdateCustomer(customer);

            //event notification
            await _eventPublisher.EntityDeleted(customer);
        }

        /// <summary>
        /// Gets a customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>A customer</returns>
        public virtual async Task<Customer> GetCustomerById(int customerId)
        {
            if (customerId == 0)
                return null;

            return await _customerRepository.ToCachedGetById(customerId, _cachingSettings.ShortTermCacheTime);
        }

        /// <summary>
        /// Get customers by identifiers
        /// </summary>
        /// <param name="customerIds">Customer identifiers</param>
        /// <returns>Customers</returns>
        public virtual async Task<IList<Customer>> GetCustomersByIds(int[] customerIds)
        {
            if (customerIds == null || customerIds.Length == 0)
                return new List<Customer>();

            var query = from c in _customerRepository.Table
                        where customerIds.Contains(c.Id) && !c.Deleted
                        select c;
            var customers = await query.ToListAsync();
            //sort by passed identifiers
            var sortedCustomers = new List<Customer>();
            foreach (var id in customerIds)
            {
                var customer = customers.Find(x => x.Id == id);
                if (customer != null)
                    sortedCustomers.Add(customer);
            }

            return sortedCustomers;
        }

        /// <summary>
        /// Gets a customer by GUID
        /// </summary>
        /// <param name="customerGuid">Customer GUID</param>
        /// <returns>A customer</returns>
        public virtual async Task<Customer> GetCustomerByGuid(Guid customerGuid)
        {
            if (customerGuid == Guid.Empty)
                return null;

            var query = from c in _customerRepository.Table
                        where c.CustomerGuid == customerGuid
                        orderby c.Id
                        select c;
            var customer = await query.FirstOrDefaultAsync();

            return customer;
        }

        /// <summary>
        /// Get customer by email
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var query = from c in _customerRepository.Table
                        orderby c.Id
                        where c.Email == email
                        select c;
            var customer = await query.FirstOrDefaultAsync();

            return customer;
        }

        /// <summary>
        /// Get customer by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerBySystemName(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
                return null;

            var query = from c in _customerRepository.Table
                        orderby c.Id
                        where c.SystemName == systemName
                        select c;
            var customer = await query.FirstOrDefaultAsync();

            return customer;
        }

        /// <summary>
        /// Gets built-in system record used for background tasks
        /// </summary>
        /// <returns>A customer object</returns>
        public virtual async Task<Customer> GetOrCreateBackgroundTaskUser()
        {
            var backgroundTaskUser = await GetCustomerBySystemName(NopCustomerDefaults.BackgroundTaskCustomerName);

            if(backgroundTaskUser is null)
            {
                //If for any reason the system user isn't in the database, then we add it
                backgroundTaskUser = new Customer
                {
                    Email = "builtin@background-task-record.com",
                    CustomerGuid = Guid.NewGuid(),
                    AdminComment = "Built-in system record used for background tasks.",
                    Active = true,
                    IsSystemAccount = true,
                    SystemName = NopCustomerDefaults.BackgroundTaskCustomerName,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastActivityDateUtc = DateTime.UtcNow,
                    RegisteredInStoreId = (await _storeContext.GetCurrentStore()).Id
                };
                
                await InsertCustomer(backgroundTaskUser);

                var guestRole = await GetCustomerRoleBySystemName(NopCustomerDefaults.GuestsRoleName);

                if(guestRole is null)
                    throw new NopException("'Guests' role could not be loaded");

                await AddCustomerRoleMapping(new CustomerCustomerRoleMapping { CustomerRoleId = guestRole.Id, CustomerId = backgroundTaskUser.Id });
            }

            return backgroundTaskUser;
        }

        /// <summary>
        /// Gets built-in system guest record used for requests from search engines
        /// </summary>
        /// <returns>A customer object</returns>
        public virtual async Task<Customer> GetOrCreateSearchEngineUser()
        {
            var searchEngineUser = await GetCustomerBySystemName(NopCustomerDefaults.SearchEngineCustomerName);

            if(searchEngineUser is null)
            {
                //If for any reason the system user isn't in the database, then we add it
                searchEngineUser = new Customer
                {
                    Email = "builtin@search_engine_record.com",
                    CustomerGuid = Guid.NewGuid(),
                    AdminComment = "Built-in system guest record used for requests from search engines.",
                    Active = true,
                    IsSystemAccount = true,
                    SystemName = NopCustomerDefaults.SearchEngineCustomerName,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastActivityDateUtc = DateTime.UtcNow,
                    RegisteredInStoreId = (await _storeContext.GetCurrentStore()).Id
                };
                
                await InsertCustomer(searchEngineUser);

                var guestRole = GetCustomerRoleBySystemName(NopCustomerDefaults.GuestsRoleName);

                if(guestRole is null)
                    throw new NopException("'Guests' role could not be loaded");

                await AddCustomerRoleMapping(new CustomerCustomerRoleMapping { CustomerRoleId = guestRole.Id, CustomerId = searchEngineUser.Id });
            }

            return searchEngineUser;
        }

        /// <summary>
        /// Get customer by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var query = from c in _customerRepository.Table
                        orderby c.Id
                        where c.Username == username
                        select c;
            var customer = await query.FirstOrDefaultAsync();

            return customer;
        }

        /// <summary>
        /// Insert a guest customer
        /// </summary>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> InsertGuestCustomer()
        {
            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow
            };

            //add to 'Guests' role
            var guestRole = await GetCustomerRoleBySystemName(NopCustomerDefaults.GuestsRoleName);
            if (guestRole == null)
                throw new NopException("'Guests' role could not be loaded");

            await _customerRepository.Insert(customer);

            //event notification
            await _eventPublisher.EntityInserted(customer);

            await AddCustomerRoleMapping(new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });

            return customer;
        }

        /// <summary>
        /// Insert a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task InsertCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            await _customerRepository.Insert(customer);

            //event notification
            await _eventPublisher.EntityInserted(customer);
        }

        /// <summary>
        /// Updates the customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task UpdateCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            await _customerRepository.Update(customer);

            //event notification
            await _eventPublisher.EntityUpdated(customer);
        }

        /// <summary>
        /// Reset data required for checkout
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="clearCouponCodes">A value indicating whether to clear coupon code</param>
        /// <param name="clearCheckoutAttributes">A value indicating whether to clear selected checkout attributes</param>
        /// <param name="clearRewardPoints">A value indicating whether to clear "Use reward points" flag</param>
        /// <param name="clearShippingMethod">A value indicating whether to clear selected shipping method</param>
        /// <param name="clearPaymentMethod">A value indicating whether to clear selected payment method</param>
        public virtual async Task ResetCheckoutData(Customer customer, int storeId,
            bool clearCouponCodes = false, bool clearCheckoutAttributes = false,
            bool clearRewardPoints = true, bool clearShippingMethod = true,
            bool clearPaymentMethod = true)
        {
            if (customer == null)
                throw new ArgumentNullException();

            //clear entered coupon codes
            if (clearCouponCodes)
            {
                await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.DiscountCouponCodeAttribute, null);
                await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute, null);
            }

            //clear checkout attributes
            if (clearCheckoutAttributes) 
                await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.CheckoutAttributes, null, storeId);

            //clear reward points flag
            if (clearRewardPoints) 
                await _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.UseRewardPointsDuringCheckoutAttribute, false, storeId);

            //clear selected shipping method
            if (clearShippingMethod)
            {
                await _genericAttributeService.SaveAttribute<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, storeId);
                await _genericAttributeService.SaveAttribute<ShippingOption>(customer, NopCustomerDefaults.OfferedShippingOptionsAttribute, null, storeId);
                await _genericAttributeService.SaveAttribute<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, storeId);
            }

            //clear selected payment method
            if (clearPaymentMethod) 
                await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, null, storeId);

            await UpdateCustomer(customer);
        }

        /// <summary>
        /// Delete guest customer records
        /// </summary>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="onlyWithoutShoppingCart">A value indicating whether to delete customers only without shopping cart</param>
        /// <returns>Number of deleted customers</returns>
        public virtual async Task<int> DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart)
        {
            //prepare parameters
            var pOnlyWithoutShoppingCart = SqlParameterHelper.GetBooleanParameter("OnlyWithoutShoppingCart", onlyWithoutShoppingCart);
            var pCreatedFromUtc = SqlParameterHelper.GetDateTimeParameter("CreatedFromUtc", createdFromUtc);
            var pCreatedToUtc = SqlParameterHelper.GetDateTimeParameter("CreatedToUtc", createdToUtc);
            var pTotalRecordsDeleted = SqlParameterHelper.GetOutputInt32Parameter("TotalRecordsDeleted");

            //invoke stored procedure
            await _customerRepository.EntityFromSql("DeleteGuests", pOnlyWithoutShoppingCart,
                pCreatedFromUtc,
                pCreatedToUtc,
                pTotalRecordsDeleted);

            var totalRecordsDeleted = pTotalRecordsDeleted.Value != DBNull.Value ? Convert.ToInt32(pTotalRecordsDeleted.Value) : 0;
            return totalRecordsDeleted;
        }

        /// <summary>
        /// Gets a default tax display type (if configured)
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public virtual async Task<TaxDisplayType?> GetCustomerDefaultTaxDisplayType(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var roleWithOverriddenTaxType = (await GetCustomerRoles(customer)).FirstOrDefault(cr => cr.Active && cr.OverrideTaxDisplayType);
            if (roleWithOverriddenTaxType == null)
                return null;

            return (TaxDisplayType)roleWithOverriddenTaxType.DefaultTaxDisplayTypeId;
        }

        /// <summary>
        /// Get full name
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Customer full name</returns>
        public virtual async Task<string> GetCustomerFullName(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var firstName = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute);
            var lastName = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.LastNameAttribute);

            var fullName = string.Empty;
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                fullName = $"{firstName} {lastName}";
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!string.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }

            return fullName;
        }

        /// <summary>
        /// Formats the customer name
        /// </summary>
        /// <param name="customer">Source</param>
        /// <param name="stripTooLong">Strip too long customer name</param>
        /// <param name="maxLength">Maximum customer name length</param>
        /// <returns>Formatted text</returns>
        public virtual async Task<string> FormatUsername(Customer customer, bool stripTooLong = false, int maxLength = 0)
        {
            if (customer == null)
                return string.Empty;

            //TODO: try to use DI
            if (await IsGuest(customer))
                return await EngineContext.Current.Resolve<ILocalizationService>().GetResource("Customer.Guest");

            var result = string.Empty;
            switch (_customerSettings.CustomerNameFormat)
            {
                case CustomerNameFormat.ShowEmails:
                    result = customer.Email;
                    break;
                case CustomerNameFormat.ShowUsernames:
                    result = customer.Username;
                    break;
                case CustomerNameFormat.ShowFullNames:
                    result = await GetCustomerFullName(customer);
                    break;
                case CustomerNameFormat.ShowFirstName:
                    result = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute);
                    break;
                default:
                    break;
            }

            if (stripTooLong && maxLength > 0)
                result = CommonHelper.EnsureMaximumLength(result, maxLength);

            return result;
        }

        /// <summary>
        /// Gets coupon codes
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Coupon codes</returns>
        public virtual async Task<string[]> ParseAppliedDiscountCouponCodes(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.DiscountCouponCodeAttribute);

            var couponCodes = new List<string>();
            if (string.IsNullOrEmpty(existingCouponCodes))
                return couponCodes.ToArray();

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(existingCouponCodes);

                var nodeList1 = xmlDoc.SelectNodes(@"//DiscountCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes?["Code"] == null)
                        continue;
                    var code = node1.Attributes["Code"].InnerText.Trim();
                    couponCodes.Add(code);
                }
            }
            catch
            {
                // ignored
            }

            return couponCodes.ToArray();
        }

        /// <summary>
        /// Adds a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public virtual async Task ApplyDiscountCouponCode(Customer customer, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var result = string.Empty;
            try
            {
                var existingCouponCodes = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.DiscountCouponCodeAttribute);

                couponCode = couponCode.Trim().ToLower();

                var xmlDoc = new XmlDocument();
                if (string.IsNullOrEmpty(existingCouponCodes))
                {
                    var element1 = xmlDoc.CreateElement("DiscountCouponCodes");
                    xmlDoc.AppendChild(element1);
                }
                else
                    xmlDoc.LoadXml(existingCouponCodes);

                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//DiscountCouponCodes");

                XmlElement gcElement = null;
                //find existing
                var nodeList1 = xmlDoc.SelectNodes(@"//DiscountCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes?["Code"] == null)
                        continue;

                    var couponCodeAttribute = node1.Attributes["Code"].InnerText.Trim();

                    if (couponCodeAttribute.ToLower() != couponCode.ToLower())
                        continue;

                    gcElement = (XmlElement)node1;
                    break;
                }

                //create new one if not found
                if (gcElement == null)
                {
                    gcElement = xmlDoc.CreateElement("CouponCode");
                    gcElement.SetAttribute("Code", couponCode);
                    rootElement.AppendChild(gcElement);
                }

                result = xmlDoc.OuterXml;
            }
            catch
            {
                // ignored
            }

            //apply new value
            await _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.DiscountCouponCodeAttribute, result);
        }

        /// <summary>
        /// Removes a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code to remove</param>
        /// <returns>New coupon codes document</returns>
        public virtual async Task RemoveDiscountCouponCode(Customer customer, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            //get applied coupon codes
            var existingCouponCodes = await ParseAppliedDiscountCouponCodes(customer);

            //clear them
            await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.DiscountCouponCodeAttribute, null);

            //save again except removed one
            foreach (var existingCouponCode in existingCouponCodes)
                if (!existingCouponCode.Equals(couponCode, StringComparison.InvariantCultureIgnoreCase))
                    await ApplyDiscountCouponCode(customer, existingCouponCode);
        }

        /// <summary>
        /// Gets coupon codes
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Coupon codes</returns>
        public virtual async Task<string[]> ParseAppliedGiftCardCouponCodes(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute);

            var couponCodes = new List<string>();
            if (string.IsNullOrEmpty(existingCouponCodes))
                return couponCodes.ToArray();

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(existingCouponCodes);

                var nodeList1 = xmlDoc.SelectNodes(@"//GiftCardCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes?["Code"] == null)
                        continue;

                    var code = node1.Attributes["Code"].InnerText.Trim();
                    couponCodes.Add(code);
                }
            }
            catch
            {
                // ignored
            }

            return couponCodes.ToArray();
        }

        /// <summary>
        /// Adds a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public virtual async Task ApplyGiftCardCouponCode(Customer customer, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var result = string.Empty;
            try
            {
                var existingCouponCodes = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute);

                couponCode = couponCode.Trim().ToLower();

                var xmlDoc = new XmlDocument();
                if (string.IsNullOrEmpty(existingCouponCodes))
                {
                    var element1 = xmlDoc.CreateElement("GiftCardCouponCodes");
                    xmlDoc.AppendChild(element1);
                }
                else
                    xmlDoc.LoadXml(existingCouponCodes);

                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//GiftCardCouponCodes");

                XmlElement gcElement = null;
                //find existing
                var nodeList1 = xmlDoc.SelectNodes(@"//GiftCardCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes?["Code"] == null)
                        continue;

                    var couponCodeAttribute = node1.Attributes["Code"].InnerText.Trim();
                    if (couponCodeAttribute.ToLower() != couponCode.ToLower())
                        continue;

                    gcElement = (XmlElement)node1;
                    break;
                }

                //create new one if not found
                if (gcElement == null)
                {
                    gcElement = xmlDoc.CreateElement("CouponCode");
                    gcElement.SetAttribute("Code", couponCode);
                    rootElement.AppendChild(gcElement);
                }

                result = xmlDoc.OuterXml;
            }
            catch
            {
                // ignored
            }

            //apply new value
            await _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute, result);
        }

        /// <summary>
        /// Removes a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code to remove</param>
        /// <returns>New coupon codes document</returns>
        public virtual async Task RemoveGiftCardCouponCode(Customer customer, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            //get applied coupon codes
            var existingCouponCodes = await ParseAppliedGiftCardCouponCodes(customer);

            //clear them
            await _genericAttributeService.SaveAttribute<string>(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute, null);

            //save again except removed one
            foreach (var existingCouponCode in existingCouponCodes)
                if (!existingCouponCode.Equals(couponCode, StringComparison.InvariantCultureIgnoreCase))
                    await ApplyGiftCardCouponCode(customer, existingCouponCode);
        }

        #endregion

        #region Customer roles

        /// <summary>
        /// Add a customer-customer role mapping
        /// </summary>
        /// <param name="roleMapping">Customer-customer role mapping</param>
        public async Task AddCustomerRoleMapping(CustomerCustomerRoleMapping roleMapping)
        {
            if (roleMapping is null)
                throw new ArgumentNullException(nameof(roleMapping));

            await _customerCustomerRoleMappingRepository.Insert(roleMapping);

            await _eventPublisher.EntityInserted(roleMapping);
        }

        /// <summary>
        /// Remove a customer-customer role mapping
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="role">Customer role</param>
        public async Task RemoveCustomerRoleMapping(Customer customer, CustomerRole role)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            if (role is null)
                throw new ArgumentNullException(nameof(role));

            var mapping = await _customerCustomerRoleMappingRepository.Table.SingleOrDefaultAsync(ccrm => ccrm.CustomerId == customer.Id && ccrm.CustomerRoleId == role.Id);

            if (mapping != null)
            {
                await _customerCustomerRoleMappingRepository.Delete(mapping);

                //event notification
                await _eventPublisher.EntityDeleted(mapping);
            }
        }

        /// <summary>
        /// Delete a customer role
        /// </summary>
        /// <param name="customerRole">Customer role</param>
        public virtual async Task DeleteCustomerRole(CustomerRole customerRole)
        {
            if (customerRole == null)
                throw new ArgumentNullException(nameof(customerRole));

            if (customerRole.IsSystemRole)
                throw new NopException("System role could not be deleted");

            await _customerRoleRepository.Delete(customerRole);

            //event notification
            await _eventPublisher.EntityDeleted(customerRole);
        }

        /// <summary>
        /// Gets a customer role
        /// </summary>
        /// <param name="customerRoleId">Customer role identifier</param>
        /// <returns>Customer role</returns>
        public virtual async Task<CustomerRole> GetCustomerRoleById(int customerRoleId)
        {
            if (customerRoleId == 0)
                return null;

            return await _customerRoleRepository.ToCachedGetById(customerRoleId);
        }

        /// <summary>
        /// Gets a customer role
        /// </summary>
        /// <param name="systemName">Customer role system name</param>
        /// <returns>Customer role</returns>
        public virtual async Task<CustomerRole> GetCustomerRoleBySystemName(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
                return null;

            var key = _cacheKeyService.PrepareKeyForDefaultCache(NopCustomerServicesDefaults.CustomerRolesBySystemNameCacheKey, systemName);

            var query = from cr in _customerRoleRepository.Table
                orderby cr.Id
                where cr.SystemName == systemName
                select cr;
            var customerRole = await query.ToCachedFirstOrDefault(key);

            return customerRole;
        }

        /// <summary>
        /// Get customer role identifiers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="showHidden">A value indicating whether to load hidden records</param>
        /// <returns>Customer role identifiers</returns>
        public virtual async Task<int[]> GetCustomerRoleIds(Customer customer, bool showHidden = false)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var query = from cr in _customerRoleRepository.Table
                        join crm in _customerCustomerRoleMappingRepository.Table on cr.Id equals crm.CustomerRoleId
                        where crm.CustomerId == customer.Id && 
                        (showHidden || cr.Active)
                        select cr.Id;

            var key = _cacheKeyService.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerRoleIdsCacheKey, customer, showHidden);

            return await _staticCacheManager.Get(key, async () => await query.ToArrayAsync());
        }

        /// <summary>
        /// Gets list of customer roles
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="showHidden">A value indicating whether to load hidden records</param>
        /// <returns>Result</returns>
        public virtual async Task<IList<CustomerRole>> GetCustomerRoles(Customer customer, bool showHidden = false)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var query = from cr in _customerRoleRepository.Table
                        join crm in _customerCustomerRoleMappingRepository.Table on cr.Id equals crm.CustomerRoleId
                        where crm.CustomerId == customer.Id && 
                        (showHidden || cr.Active)
                        select cr;

            var key = _cacheKeyService.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerRolesCacheKey, customer, showHidden);

            return  await _staticCacheManager.Get(key, async () => await query.ToListAsync());
        }

        /// <summary>
        /// Gets all customer roles
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Customer roles</returns>
        public virtual async Task<IList<CustomerRole>> GetAllCustomerRoles(bool showHidden = false)
        {
            var key = _cacheKeyService.PrepareKeyForDefaultCache(NopCustomerServicesDefaults.CustomerRolesAllCacheKey, showHidden);

            var query = from cr in _customerRoleRepository.Table
                orderby cr.Name
                where showHidden || cr.Active
                select cr;

            var customerRoles = await query.ToCachedList(key);

            return customerRoles;
        }

        /// <summary>
        /// Inserts a customer role
        /// </summary>
        /// <param name="customerRole">Customer role</param>
        public virtual async Task InsertCustomerRole(CustomerRole customerRole)
        {
            if (customerRole == null)
                throw new ArgumentNullException(nameof(customerRole));

            await _customerRoleRepository.Insert(customerRole);

            //event notification
            await _eventPublisher.EntityInserted(customerRole);
        }

        /// <summary>
        /// Gets a value indicating whether customer is in a certain customer role
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="customerRoleSystemName">Customer role system name</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsInCustomerRole(Customer customer,
            string customerRoleSystemName, bool onlyActiveCustomerRoles = true)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (string.IsNullOrEmpty(customerRoleSystemName))
                throw new ArgumentNullException(nameof(customerRoleSystemName));

            var customerRoles = await GetCustomerRoles(customer, !onlyActiveCustomerRoles);

            return customerRoles?.Any(cr => cr.SystemName == customerRoleSystemName) ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether customer is administrator
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsAdmin(Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return await IsInCustomerRole(customer, NopCustomerDefaults.AdministratorsRoleName, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsForumModerator(Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return await IsInCustomerRole(customer, NopCustomerDefaults.ForumModeratorsRoleName, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is registered
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsRegistered(Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return await IsInCustomerRole(customer, NopCustomerDefaults.RegisteredRoleName, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is guest
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsGuest(Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return await IsInCustomerRole(customer, NopCustomerDefaults.GuestsRoleName, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is vendor
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsVendor(Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return await IsInCustomerRole(customer, NopCustomerDefaults.VendorsRoleName, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Updates the customer role
        /// </summary>
        /// <param name="customerRole">Customer role</param>
        public virtual async Task UpdateCustomerRole(CustomerRole customerRole)
        {
            if (customerRole == null)
                throw new ArgumentNullException(nameof(customerRole));

            await _customerRoleRepository.Update(customerRole);

            //event notification
            await _eventPublisher.EntityUpdated(customerRole);
        }

        #endregion

        #region Customer passwords

        /// <summary>
        /// Gets customer passwords
        /// </summary>
        /// <param name="customerId">Customer identifier; pass null to load all records</param>
        /// <param name="passwordFormat">Password format; pass null to load all records</param>
        /// <param name="passwordsToReturn">Number of returning passwords; pass null to load all records</param>
        /// <returns>List of customer passwords</returns>
        public virtual async Task<IList<CustomerPassword>> GetCustomerPasswords(int? customerId = null,
            PasswordFormat? passwordFormat = null, int? passwordsToReturn = null)
        {
            var query = _customerPasswordRepository.Table;

            //filter by customer
            if (customerId.HasValue)
                query = query.Where(password => password.CustomerId == customerId.Value);

            //filter by password format
            if (passwordFormat.HasValue)
                query = query.Where(password => password.PasswordFormatId == (int)passwordFormat.Value);

            //get the latest passwords
            if (passwordsToReturn.HasValue)
                query = query.OrderByDescending(password => password.CreatedOnUtc).Take(passwordsToReturn.Value);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Get current customer password
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Customer password</returns>
        public virtual async Task<CustomerPassword> GetCurrentPassword(int customerId)
        {
            if (customerId == 0)
                return null;

            //return the latest password
            return (await GetCustomerPasswords(customerId, passwordsToReturn: 1)).FirstOrDefault();
        }

        /// <summary>
        /// Insert a customer password
        /// </summary>
        /// <param name="customerPassword">Customer password</param>
        public virtual async Task InsertCustomerPassword(CustomerPassword customerPassword)
        {
            if (customerPassword == null)
                throw new ArgumentNullException(nameof(customerPassword));

            await _customerPasswordRepository.Insert(customerPassword);

            //event notification
            await _eventPublisher.EntityInserted(customerPassword);
        }

        /// <summary>
        /// Update a customer password
        /// </summary>
        /// <param name="customerPassword">Customer password</param>
        public virtual async Task UpdateCustomerPassword(CustomerPassword customerPassword)
        {
            if (customerPassword == null)
                throw new ArgumentNullException(nameof(customerPassword));

            await _customerPasswordRepository.Update(customerPassword);

            //event notification
            await _eventPublisher.EntityUpdated(customerPassword);
        }

        /// <summary>
        /// Check whether password recovery token is valid
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="token">Token to validate</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsPasswordRecoveryTokenValid(Customer customer, string token)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var cPrt = await _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute);
            if (string.IsNullOrEmpty(cPrt))
                return false;

            if (!cPrt.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Check whether password recovery link is expired
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsPasswordRecoveryLinkExpired(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (_customerSettings.PasswordRecoveryLinkDaysValid == 0)
                return false;

            var generatedDate = await _genericAttributeService.GetAttribute<DateTime?>(customer, NopCustomerDefaults.PasswordRecoveryTokenDateGeneratedAttribute);
            if (!generatedDate.HasValue)
                return false;

            var daysPassed = (DateTime.UtcNow - generatedDate.Value).TotalDays;
            if (daysPassed > _customerSettings.PasswordRecoveryLinkDaysValid)
                return true;

            return false;
        }

        /// <summary>
        /// Check whether customer password is expired 
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>True if password is expired; otherwise false</returns>
        public virtual async Task<bool> PasswordIsExpired(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            //the guests don't have a password
            if (await IsGuest(customer))
                return false;

            //password lifetime is disabled for user
            if (!(await GetCustomerRoles(customer)).Any(role => role.Active && role.EnablePasswordLifetime))
                return false;

            //setting disabled for all
            if (_customerSettings.PasswordLifetime == 0)
                return false;

            var cacheKey = _cacheKeyService.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerPasswordLifetimeCacheKey, customer);
            
            //get current password usage time
            var currentLifetime = await _staticCacheManager.Get(cacheKey, async () =>
            {
                var customerPassword = await GetCurrentPassword(customer.Id);
                //password is not found, so return max value to force customer to change password
                if (customerPassword == null)
                    return int.MaxValue;

                return (DateTime.UtcNow - customerPassword.CreatedOnUtc).Days;
            });

            return currentLifetime >= _customerSettings.PasswordLifetime;
        }

        #endregion

        #region Customer address mapping

        /// <summary>
        /// Remove a customer-address mapping record
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="address">Address</param>
        public virtual async Task RemoveCustomerAddress(Customer customer, Address address)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (await _customerAddressMappingRepository.Table.FirstOrDefaultAsync(m => m.AddressId == address.Id && m.CustomerId == customer.Id) is CustomerAddressMapping mapping)
            {
                if (customer.BillingAddressId == address.Id)
                    customer.BillingAddressId = null;
                if (customer.ShippingAddressId == address.Id)
                    customer.ShippingAddressId = null;

                await _customerAddressMappingRepository.Delete(mapping);

                //event notification
                await _eventPublisher.EntityDeleted(mapping);
            }
        }

        /// <summary>
        /// Inserts a customer-address mapping record
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="address">Address</param>
        public virtual async Task InsertCustomerAddress(Customer customer, Address address)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            if (address is null)
                throw new ArgumentNullException(nameof(address));

            if (await _customerAddressMappingRepository.Table.FirstOrDefaultAsync(m => m.AddressId == address.Id && m.CustomerId == customer.Id) is null)
            {
                var mapping = new CustomerAddressMapping
                {
                    AddressId = address.Id,
                    CustomerId = customer.Id
                };

                await _customerAddressMappingRepository.Insert(mapping);

                //event notification
                await _eventPublisher.EntityInserted(mapping);
            }
        }

        /// <summary>
        /// Gets a list of addresses mapped to customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Result</returns>
        public virtual async Task<IList<Address>> GetAddressesByCustomerId(int customerId)
        {
            var query = from address in _customerAddressRepository.Table
                join cam in _customerAddressMappingRepository.Table on address.Id equals cam.AddressId
                where cam.CustomerId == customerId
                select address;

            var key = _cacheKeyService.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerAddressesByCustomerIdCacheKey, customerId);

            return await _staticCacheManager.Get(key, async () => await query.ToListAsync());
        }

        /// <summary>
        /// Gets a address mapped to customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="addressId">Address identifier</param>
        /// <returns>Result</returns>
        public virtual async Task<Address> GetCustomerAddress(int customerId, int addressId)
        {
            if (customerId == 0 || addressId == 0)
                return null;

            var query = from address in _customerAddressRepository.Table
                join cam in _customerAddressMappingRepository.Table on address.Id equals cam.AddressId
                where cam.CustomerId == customerId && address.Id == addressId
                select address;

            var key = _cacheKeyService.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerAddressCacheKeyCacheKey, customerId, addressId);

            return await _staticCacheManager.Get(key, async () => await query.SingleAsync());
        }

        /// <summary>
        /// Gets a customer billing address
        /// </summary>
        /// <param name="customer">Customer identifier</param>
        /// <returns>Result</returns>
        public virtual async Task<Address> GetCustomerBillingAddress(Customer customer)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            return await GetCustomerAddress(customer.Id, customer.BillingAddressId ?? 0);
        }

        /// <summary>
        /// Gets a customer shipping address
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public virtual async Task<Address> GetCustomerShippingAddress(Customer customer)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            return await GetCustomerAddress(customer.Id, customer.ShippingAddressId ?? 0);
        }

        #endregion

        #endregion
    }
}