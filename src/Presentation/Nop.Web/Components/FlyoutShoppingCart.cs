﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Services.Security;
using Nop.Web.Factories;

namespace Nop.Web.Components
{
    public class FlyoutShoppingCartViewComponent : ViewComponent
    {
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public FlyoutShoppingCartViewComponent(IPermissionService permissionService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            ShoppingCartSettings shoppingCartSettings)
        {
            this._permissionService = permissionService;
            this._shoppingCartModelFactory = shoppingCartModelFactory;
            this._shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content("");

            if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                return Content("");

            var model = _shoppingCartModelFactory.PrepareMiniShoppingCartModel();
            return View(model);
        }
    }
}
