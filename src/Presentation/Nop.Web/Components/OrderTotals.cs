﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components
{
    public class OrderTotalsViewComponent : NopViewComponent
    {
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public OrderTotalsViewComponent(IShoppingCartModelFactory shoppingCartModelFactory,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> Invoke(bool isEditable)
        {
            var cart = await _shoppingCartService.GetShoppingCart(await _workContext.GetCurrentCustomer(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStore()).Id);

            var model = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, isEditable);
            return View(model);
        }
    }
}
