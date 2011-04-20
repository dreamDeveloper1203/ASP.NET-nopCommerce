﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog
{
    public class ProductModel : BaseNopEntityModel
    {
        public ProductModel()
        {
            ProductPrice = new ProductPriceModel();
        }

        public string ImageUrl { get; set; }

        public string Name { get; set; }

        public ProductPriceModel ProductPrice { get; set; }

        public string SeName { get; set; }

        public string ShortDescription { get; set; }


		#region Nested Classes 
        
        public class ProductPriceModel
        {
            public bool CallForPrice { get; set; }

            public bool CustomerEntersPrice { get; set; }

            public bool HasMultipleVariants { get; set; }

            public decimal? OldPrice {get;set;}

            public decimal? Price {get;set;}
        }

		#endregion Nested Classes 
    }
}