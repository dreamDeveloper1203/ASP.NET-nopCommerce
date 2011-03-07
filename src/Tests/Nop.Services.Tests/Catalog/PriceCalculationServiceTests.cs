﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Nop.Services.Tests.Catalog
{
    [TestFixture]
    public class PriceCalculationServiceTests
    {
        IWorkContext _workContext;
        IDiscountService _discountService;
        ICategoryService _categoryService;
        IProductAttributeParser _productAttributeParser;
        IPriceCalculationService _priceCalcService;

        [SetUp]
        public void SetUp()
        {
            _workContext = null;

            _discountService = MockRepository.GenerateMock<IDiscountService>();

            _categoryService = MockRepository.GenerateMock<ICategoryService>();
            _categoryService.Expect(cs => cs.GetProductCategoriesByProductId(1)).Return(new List<ProductCategory>());

            _productAttributeParser = MockRepository.GenerateMock<IProductAttributeParser>();

            _priceCalcService = new PriceCalculationService(_workContext, _discountService,
                _categoryService, _productAttributeParser);
        }

        [Test]
        public void Can_get_final_product_price()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer
            Customer customer = null;

            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 1).ShouldEqual(12.34M);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 2).ShouldEqual(12.34M);
        }

        [Test]
        public void Can_get_final_product_price_with_tier_prices()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //add tier prices
            productVariant.TierPrices.Add(new TierPrice()
                {
                    Price = 10,
                    Quantity = 2,
                    ProductVariant = productVariant
                });
            productVariant.TierPrices.Add(new TierPrice()
            {
                Price = 8,
                Quantity = 5,
                ProductVariant = productVariant
            });

            //customer
            Customer customer = null;

            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 1).ShouldEqual(12.34M);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 2).ShouldEqual(10);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 3).ShouldEqual(10);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 5).ShouldEqual(8);
        }

        [Test]
        public void Can_get_final_product_price_with_tier_prices_by_customerRole()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer roles
            var customerRole1 = new CustomerRole()
            {
                Id = 1,
                Name = "Some role 1",
                Active = true,
            };
            var customerRole2 = new CustomerRole()
            {
                Id = 2,
                Name = "Some role 2",
                Active = true,
            };

            //add tier prices
            productVariant.TierPrices.Add(new TierPrice()
            {
                Price = 10,
                Quantity = 2,
                ProductVariant = productVariant,
                CustomerRole = customerRole1
            });
            productVariant.TierPrices.Add(new TierPrice()
            {
                Price = 9,
                Quantity = 2,
                ProductVariant = productVariant,
                CustomerRole = customerRole2
            });
            productVariant.TierPrices.Add(new TierPrice()
            {
                Price = 8,
                Quantity = 5,
                ProductVariant = productVariant,
                CustomerRole = customerRole1
            });
            productVariant.TierPrices.Add(new TierPrice()
            {
                Price = 5,
                Quantity = 10,
                ProductVariant = productVariant,
                CustomerRole = customerRole2
            });

            //customer
            Customer customer = new Customer();
            customer.CustomerRoles.Add(customerRole1);

            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 1).ShouldEqual(12.34M);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 2).ShouldEqual(10);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 3).ShouldEqual(10);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 5).ShouldEqual(8);
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, false, 10).ShouldEqual(8);
        }

        [Test]
        public void Can_get_final_product_price_with_additionalFee()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer
            Customer customer = null;

            _priceCalcService.GetFinalPrice(productVariant, customer, 5, false, 1).ShouldEqual(17.34M);
        }

        [Test]
        public void Can_get_final_product_price_with_discount()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer
            Customer customer = null;

            //discounts
            var discount1 = new Discount()
            {
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                AppliedToProductVariants = new List<ProductVariant>() { productVariant }
            };
            productVariant.AppliedDiscounts.Add(discount1);
            _discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);
            
            _priceCalcService.GetFinalPrice(productVariant, customer, 0, true, 1).ShouldEqual(9.34M);
        }

        [Test]
        public void Can_get_product_discount()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer
            Customer customer = null;

            //discounts
            var discount1 = new Discount()
            {
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                AppliedToProductVariants = new List<ProductVariant>() { productVariant }
            };
            productVariant.AppliedDiscounts.Add(discount1);
            _discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);

            var discount2 = new Discount()
            {
                Name = "Discount 2",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 4,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                AppliedToProductVariants = new List<ProductVariant>() { productVariant }
            };
            productVariant.AppliedDiscounts.Add(discount2);
            _discountService.Expect(ds => ds.IsDiscountValid(discount2, customer)).Return(true);

            var discount3 = new Discount()
            {
                Name = "Discount 3",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountAmount = 5,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                RequiresCouponCode = true,
                CouponCode = "SECRET CODE",
                AppliedToProductVariants = new List<ProductVariant>() { productVariant }
            };
            productVariant.AppliedDiscounts.Add(discount2);
            //discount is not valid
            _discountService.Expect(ds => ds.IsDiscountValid(discount2, customer)).Return(false);


            Discount appliedDiscount;
            _priceCalcService.GetDiscountAmount(productVariant, customer, 0, 1, out appliedDiscount).ShouldEqual(4);
            appliedDiscount.ShouldEqual(discount2);
        }

        [Test]
        public void Ensure_discount_is_not_applied_to_products_with_prices_entered_by_customer()
        {
            var productVariant = new ProductVariant
            {
                Id = 1,
                Name = "Product variant name 1",
                Price = 12.34M,
                CustomerEntersPrice = true,
                Published = true,
                Product = new Product()
                {
                    Id = 1,
                    Name = "Product name 1",
                    Published = true
                }
            };

            //customer
            Customer customer = null;

            //discounts
            var discount1 = new Discount()
            {
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                AppliedToProductVariants = new List<ProductVariant>() { productVariant }
            };
            productVariant.AppliedDiscounts.Add(discount1);
            _discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);
            
            Discount appliedDiscount;
            _priceCalcService.GetDiscountAmount(productVariant, customer, 0, 1, out appliedDiscount).ShouldEqual(0);
            appliedDiscount.ShouldBeNull();
        }
    }
}
