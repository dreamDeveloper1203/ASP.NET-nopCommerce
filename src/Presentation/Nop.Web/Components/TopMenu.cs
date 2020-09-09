﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components
{
    public class TopMenuViewComponent : NopViewComponent
    {
        private readonly ICatalogModelFactory _catalogModelFactory;

        public TopMenuViewComponent(ICatalogModelFactory catalogModelFactory)
        {
            _catalogModelFactory = catalogModelFactory;
        }

        public async Task<IViewComponentResult> Invoke(int? productThumbPictureSize)
        {
            var model = await _catalogModelFactory.PrepareTopMenuModel();
            return View(model);
        }
    }
}
