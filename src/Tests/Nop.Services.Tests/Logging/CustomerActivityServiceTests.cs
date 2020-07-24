﻿using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Core.Events;
using Nop.Services.Logging;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Logging
{
    [TestFixture]
    public class CustomerActivityServiceTests : ServiceTest
    {
        private Mock<IEventPublisher> _eventPublisher;
        private FakeRepository<ActivityLog> _activityLogRepository;
        private FakeRepository<ActivityLogType> _activityLogTypeRepository;
        private Mock<IWorkContext> _workContext;
        private ICustomerActivityService _customerActivityService;
        private ActivityLogType _activityType1, _activityType2;
        private ActivityLog _activity1, _activity2;
        private Customer _customer1, _customer2;
        private Mock<IWebHelper> _webHelper;

        [SetUp]
        public new void SetUp()
        {
            _activityType1 = new ActivityLogType
            {
                Id = 1,
                SystemKeyword = "TestKeyword1",
                Enabled = true,
                Name = "Test name1"
            };
            _activityType2 = new ActivityLogType
            {
                Id = 2,
                SystemKeyword = "TestKeyword2",
                Enabled = true,
                Name = "Test name2"
            };
            _customer1 = new Customer
            {
                Id = 1,
                Email = "test1@teststore1.com",
                Username = "TestUser1",
                Deleted = false
            };
           _customer2 = new Customer
           {
               Id = 2,
               Email = "test2@teststore2.com",
               Username = "TestUser2",
               Deleted = false
           };
            _activity1 = new ActivityLog
            {
                Id = 1,
                ActivityLogTypeId = _activityType1.Id,
                CustomerId = _customer1.Id
            };
            _activity2 = new ActivityLog
            {
                Id = 2,
                ActivityLogTypeId = _activityType2.Id,
                CustomerId = _customer2.Id
            };

            _eventPublisher = new Mock<IEventPublisher>();
            _eventPublisher.Setup(x => x.Publish(It.IsAny<object>()));

            _workContext = new Mock<IWorkContext>();
            _webHelper = new Mock<IWebHelper>();
            _activityLogRepository = new FakeRepository<ActivityLog>(new List<ActivityLog> { _activity1, _activity2 });
            _activityLogTypeRepository = new FakeRepository<ActivityLogType>(new List<ActivityLogType> { _activityType1, _activityType2 });

            _customerActivityService = new CustomerActivityService(_activityLogRepository, _activityLogTypeRepository, new TestCacheManager(), _webHelper.Object, _workContext.Object);
        }

        [Test]
        public void Can_Find_Activities()
        {
            var activities = _customerActivityService.GetAllActivities(customerId: 1, pageSize: 10);
            activities.Contains(_activity1).Should().BeTrue();

            activities = _customerActivityService.GetAllActivities(customerId: 2, pageSize: 10);
            activities.Contains(_activity1).Should().BeFalse();

            activities = _customerActivityService.GetAllActivities(customerId: 2, pageSize: 10);
            activities.Contains(_activity2).Should().BeTrue();
        }
    }
}
