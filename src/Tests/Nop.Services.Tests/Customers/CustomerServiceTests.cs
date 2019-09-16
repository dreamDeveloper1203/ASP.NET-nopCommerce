﻿using System.Collections.Generic;
using System.Linq;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Customers
{
    [TestFixture]
    public class CustomerServiceTests
    {
        private readonly FakeDataStore _fakeDataStore = new FakeDataStore();

        private ICustomerService _customerService;
        private IRepository<Customer> _customerRepo;
        private IRepository<CustomerRole> _customerRoleRepo;
        private IRepository<CustomerCustomerRoleMapping> _customerCustomerRoleMapping;
        private IRepository<Address> _customerAddressRepo;
        private IRepository<CustomerAddressMapping> _customerAddressMappingRepo;
        private IRepository<GenericAttribute> _genericAttributeRepo;
        private IRepository<ShoppingCartItem> _shoppingCartRepo;

        private CustomerRole _customerRoleAdmin = new CustomerRole
        {
            Id = 1,
            Active = true,
            Name = "Administrators",
            SystemName = NopCustomerDefaults.AdministratorsRoleName
        };

        private CustomerRole _customerRoleGuests = new CustomerRole
        {
            Id = 2,
            Active = true,
            Name = "Guests",
            SystemName = NopCustomerDefaults.GuestsRoleName
        };

        private CustomerRole _customerRoleRegistered = new CustomerRole
        {
            Id = 3,
            Active = true,
            Name = "Registered",
            SystemName = NopCustomerDefaults.RegisteredRoleName
        };

        private CustomerRole _customerRoleForumModerators = new CustomerRole
        {
            Id = 4,
            Active = true,
            Name = "ForumModerators",
            SystemName = NopCustomerDefaults.ForumModeratorsRoleName
        };

        private CustomerRole _customerRole1 = new CustomerRole
        {
            Id = 5,
            Active = true,
            Name = "Test name 1",
            SystemName = "Test system name 1"
        };

        private CustomerRole _customerRole2 = new CustomerRole
        {
            Id = 6,
            Active = false,
            Name = "Test name 2",
            SystemName = "Test system name 2"
        };

        public CustomerServiceTests()
        {
            _customerAddressRepo = _fakeDataStore.RegRepository(
                new List<Address>
                {
                    new Address { Id = 1 }
                });

            _customerRepo = _fakeDataStore.RegRepository(
                new List<Customer>
                {
                    new Customer() { Id = 1 }
                });



        _customerAddressMappingRepo = _fakeDataStore.RegRepository<CustomerAddressMapping>();
        
        _genericAttributeRepo = _fakeDataStore.RegRepository<GenericAttribute>();
        _shoppingCartRepo = _fakeDataStore.RegRepository<ShoppingCartItem>();

        _customerRoleRepo = _fakeDataStore.RegRepository(new FakeRepository<CustomerRole>(Roles()));
            _customerCustomerRoleMapping = _fakeDataStore.RegRepository<CustomerCustomerRoleMapping>();


            _customerService = new FakeCustomerService(
                customerRepository: _customerRepo,
                customerAddressRepository: _customerAddressRepo,
                customerAddressMappingRepository: _customerAddressMappingRepo,
                customerRoleRepository: _customerRoleRepo,
                customerCustomerRoleMappingRepository: _customerCustomerRoleMapping,
                gaRepository: _genericAttributeRepo
                );
        }

        [SetUp]
        public void SetUp()
        {
            //TODO: here we can cleaning repos after each test
        }

        private IEnumerable<CustomerRole> Roles()
        {
            return new List<CustomerRole> {
                _customerRoleAdmin,
                _customerRoleGuests,
                _customerRoleRegistered,
                _customerRoleForumModerators,
                _customerRole1,
                _customerRole2
            };
        }

        [Test]
        public void Can_check_IsInCustomerRole()
        {
            var customer = new Customer() { Id = 1 };

            var rm = new List<CustomerCustomerRoleMapping> {
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRole1.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRole2.Id, CustomerId = customer.Id }
            };

            _customerCustomerRoleMapping.Insert(rm);


            _customerService.IsInCustomerRole(customer, "Test system name 1", false).ShouldBeTrue();
            _customerService.IsInCustomerRole(customer, "Test system name 1").ShouldBeTrue();

            _customerService.IsInCustomerRole(customer, "Test system name 2", false).ShouldBeTrue();
            _customerService.IsInCustomerRole(customer, "Test system name 2").ShouldBeFalse();

            _customerService.IsInCustomerRole(customer, "Test system name 3", false).ShouldBeFalse();
            _customerService.IsInCustomerRole(customer, "Test system name 3").ShouldBeFalse();

            _customerCustomerRoleMapping.Delete(rm);
        }
        [Test]
        public void Can_check_whether_customer_is_admin()
        {
            var customer = new Customer() { Id = 1 };

            var rm = new List<CustomerCustomerRoleMapping> {
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleRegistered.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleGuests.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleAdmin.Id, CustomerId = customer.Id }
            };

            _customerCustomerRoleMapping.Insert(rm);

            _customerService.IsAdmin(customer).ShouldBeTrue();

            _customerCustomerRoleMapping.Delete(rm);
        }

        [Test]
        public void Can_check_whether_customer_is_forum_moderator()
        {
            var customer = new Customer() { Id = 1 };

            var rm = new List<CustomerCustomerRoleMapping> {
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleRegistered.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleGuests.Id, CustomerId = customer.Id }
            };

            _customerCustomerRoleMapping.Insert(rm);

            _customerService.IsForumModerator(customer).ShouldBeFalse();

            var rmForumModerators = new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleForumModerators.Id, CustomerId = customer.Id };

            _customerCustomerRoleMapping.Insert(rmForumModerators);

            _customerService.IsForumModerator(customer).ShouldBeTrue();

            _customerCustomerRoleMapping.Delete(rm);
            _customerCustomerRoleMapping.Delete(rmForumModerators);
        }

        [Test]
        public void Can_check_whether_customer_is_guest()
        {
            var customer = new Customer() { Id = 1 };

            var rm = new List<CustomerCustomerRoleMapping> {
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleRegistered.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleAdmin.Id, CustomerId = customer.Id }
            };

            _customerCustomerRoleMapping.Insert(rm);

            _customerService.IsGuest(customer).ShouldBeFalse();

            var rmRoleGuest = new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleGuests.Id, CustomerId = customer.Id };

            _customerCustomerRoleMapping.Insert(rmRoleGuest);

            _customerService.IsGuest(customer).ShouldBeTrue();

            _customerCustomerRoleMapping.Delete(rm);
            _customerCustomerRoleMapping.Delete(rmRoleGuest);
        }
        [Test]
        public void Can_check_whether_customer_is_registered()
        {
            var customer = new Customer();

            var rm = new List<CustomerCustomerRoleMapping> {
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleGuests.Id, CustomerId = customer.Id },
                new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleAdmin.Id, CustomerId = customer.Id }
            };

            _customerCustomerRoleMapping.Insert(rm);

            _customerService.IsRegistered(customer).ShouldBeFalse();

            var rmRoleRegistered = new CustomerCustomerRoleMapping { CustomerRoleId = _customerRoleRegistered.Id, CustomerId = customer.Id };

            _customerCustomerRoleMapping.Insert(rmRoleRegistered);

            _customerService.IsRegistered(customer).ShouldBeTrue();

            _customerCustomerRoleMapping.Delete(rm);
            _customerCustomerRoleMapping.Delete(rmRoleRegistered);
        }

        [Test]
        public void Can_remove_address_assigned_as_billing_address()
        {
            var customer = _customerRepo.GetById(1);
            var address = _customerAddressRepo.GetById(1);


            _customerService.InsertCustomerAddress(customer, address);

            _customerService.GetAddressesByCustomerId(customer.Id).Count().ShouldEqual(1);

            _customerService.InsertCustomerAddress(customer, address);

            _customerService.GetAddressesByCustomerId(customer.Id).Count().ShouldEqual(1);

            customer.BillingAddressId = address.Id;

            _customerService.GetCustomerBillingAddress(customer).ShouldNotBeNull();

            _customerService.GetCustomerBillingAddress(customer).Id.ShouldEqual(address.Id);

            _customerService.RemoveCustomerAddress(customer, address);

            _customerService.GetAddressesByCustomerId(customer.Id).Count.ShouldEqual(0);

            customer.BillingAddressId.ShouldBeNull();
        }

        [Test]
        public void Can_add_rewardPointsHistoryEntry()
        {
            //TODO temporary disabled until we can inject (not resolve using DI) "RewardPointsSettings" into "LimitPerStore" method of CustomerExtensions

            //var customer = new Customer();
            //customer.AddRewardPointsHistoryEntry(1, 0, "Points for registration");

            //customer.RewardPointsHistory.Count.ShouldEqual(1);
            //customer.RewardPointsHistory.First().Points.ShouldEqual(1);
        }

        [Test]
        public void Can_get_rewardPointsHistoryBalance()
        {
            //TODO temporary disabled until we can inject (not resolve using DI) "RewardPointsSettings" into "LimitPerStore" method of CustomerExtensions

            //var customer = new Customer();
            //customer.AddRewardPointsHistoryEntry(1, 0, "Points for registration");

            //customer.GetRewardPointsBalance(0).ShouldEqual(1);
        }
    }
}
