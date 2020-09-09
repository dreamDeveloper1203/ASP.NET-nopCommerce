﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Models;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class FixedOrByCountryStateZipController : BasePluginController
    {
        #region Fields

        private readonly FixedOrByCountryStateZipTaxSettings _countryStateZipSettings;
        private readonly ICountryService _countryService;
        private readonly ICountryStateZipService _taxRateService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreService _storeService;
        private readonly ITaxCategoryService _taxCategoryService;

        #endregion

        #region Ctor

        public FixedOrByCountryStateZipController(FixedOrByCountryStateZipTaxSettings countryStateZipSettings,
            ICountryService countryService,
            ICountryStateZipService taxRateService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            IStoreService storeService,
            ITaxCategoryService taxCategoryService)
        {
            _countryStateZipSettings = countryStateZipSettings;
            _countryService = countryService;
            _taxRateService = taxRateService;
            _permissionService = permissionService;
            _settingService = settingService;
            _stateProvinceService = stateProvinceService;
            _storeService = storeService;
            _taxCategoryService = taxCategoryService;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            var taxCategories = await _taxCategoryService.GetAllTaxCategories();
            if (!taxCategories.Any())
                return Content("No tax categories can be loaded");

            var model = new ConfigurationModel { CountryStateZipEnabled = _countryStateZipSettings.CountryStateZipEnabled };
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            var stores = await _storeService.GetAllStores();
            foreach (var s in stores)
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //tax categories
            foreach (var tc in taxCategories)
                model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString() });
            //countries
            var countries = await _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            var defaultCountry = countries.FirstOrDefault();
            if (defaultCountry != null)
            {
                var states = await _stateProvinceService.GetStateProvincesByCountryId(defaultCountry.Id);
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            }

            return View("~/Plugins/Tax.FixedOrByCountryStateZip/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveMode(bool value)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            //save settings
            _countryStateZipSettings.CountryStateZipEnabled = value;
            await _settingService.SaveSetting(_countryStateZipSettings);

            return Json(new { Result = true });
        }

        #region Fixed tax

        [HttpPost]
        public async Task<IActionResult> FixedRatesList(ConfigurationModel searchModel)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedDataTablesJson();

            var categories = (await _taxCategoryService.GetAllTaxCategories()).ToPagedList(searchModel);

            var gridModel = new FixedTaxRateListModel().PrepareToGrid(searchModel, categories, () =>
            {
                return categories.Select(taxCategory => new FixedTaxRateModel
                {
                    TaxCategoryId = taxCategory.Id,
                    TaxCategoryName = taxCategory.Name,
                    Rate = _settingService.GetSettingByKey<decimal>(
                        string.Format(FixedOrByCountryStateZipDefaults.FixedRateSettingsKey, taxCategory.Id)).Result
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> FixedRateUpdate(FixedTaxRateModel model)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");
            
            await _settingService.SetSetting(string.Format(FixedOrByCountryStateZipDefaults.FixedRateSettingsKey, model.TaxCategoryId), model.Rate);

            return new NullJsonResult();
        }
        
        #endregion

        #region Tax by country/state/zip

        [HttpPost]
        public async Task<IActionResult> RatesByCountryStateZipList(ConfigurationModel searchModel)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedDataTablesJson();

            var records = await _taxRateService.GetAllTaxRates(searchModel.Page - 1, searchModel.PageSize);
            
            var gridModel = new CountryStateZipListModel().PrepareToGrid(searchModel, records, () =>
            {
                return records.Select(record => new CountryStateZipModel
                {
                    Id = record.Id,
                    StoreId = record.StoreId,
                    StoreName = _storeService.GetStoreById(record.StoreId).Result?.Name ?? "*",
                    TaxCategoryId = record.TaxCategoryId,
                    TaxCategoryName = _taxCategoryService.GetTaxCategoryById(record.TaxCategoryId).Result?.Name ?? string.Empty,
                    CountryId = record.CountryId,
                    CountryName = _countryService.GetCountryById(record.CountryId).Result?.Name ?? "Unavailable",
                    StateProvinceId = record.StateProvinceId,
                    StateProvinceName = _stateProvinceService.GetStateProvinceById(record.StateProvinceId).Result?.Name ?? "*",
                    Zip = !string.IsNullOrEmpty(record.Zip) ? record.Zip : "*",
                    Percentage = record.Percentage
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddRateByCountryStateZip(ConfigurationModel model)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");
            
            await _taxRateService.InsertTaxRate(new TaxRate
            {
                StoreId = model.AddStoreId,
                TaxCategoryId = model.AddTaxCategoryId,
                CountryId = model.AddCountryId,
                StateProvinceId = model.AddStateProvinceId,
                Zip = model.AddZip,
                Percentage = model.AddPercentage
            });

            return Json(new { Result = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRateByCountryStateZip(CountryStateZipModel model)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateById(model.Id);
            taxRate.Zip = model.Zip == "*" ? null : model.Zip;
            taxRate.Percentage = model.Percentage;
            await _taxRateService.UpdateTaxRate(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRateByCountryStateZip(int id)
        {
            if (!await _permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateById(id);
            if (taxRate != null)
                await _taxRateService.DeleteTaxRate(taxRate);

            return new NullJsonResult();
        }

        #endregion

        #endregion
    }
}