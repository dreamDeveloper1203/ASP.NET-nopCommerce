﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Customers;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components
{
    public class ProfileInfoViewComponent : NopViewComponent
    {
        private readonly ICustomerService _customerService;
        private readonly IProfileModelFactory _profileModelFactory;

        public ProfileInfoViewComponent(ICustomerService customerService, IProfileModelFactory profileModelFactory)
        {
            _customerService = customerService;
            _profileModelFactory = profileModelFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerProfileId)
        {
            var customer = await _customerService.GetCustomerById(customerProfileId);
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var model = _profileModelFactory.PrepareProfileInfoModel(customer);
            return View(model);
        }
    }
}
