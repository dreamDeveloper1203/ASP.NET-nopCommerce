﻿using LinqToDB.Mapping;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    /// <summary>
    /// Represents a shopping cart item mapping configuration
    /// </summary>
    public partial class ShoppingCartItemMap : NopEntityTypeConfiguration<ShoppingCartItem>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityMappingBuilder<ShoppingCartItem> builder)
        {
            builder.HasTableName(nameof(ShoppingCartItem));

            builder.Property(item => item.CustomerEnteredPrice).HasDbType("decimal(18, 4)");

            builder.Property(item => item.StoreId);
            builder.Property(item => item.ShoppingCartTypeId);
            builder.Property(item => item.CustomerId);
            builder.Property(item => item.ProductId);
            builder.Property(item => item.AttributesXml);
            builder.Property(item => item.Quantity);
            builder.Property(item => item.RentalStartDateUtc);
            builder.Property(item => item.RentalEndDateUtc);
            builder.Property(item => item.CreatedOnUtc);
            builder.Property(item => item.UpdatedOnUtc);

            builder.Ignore(item => item.ShoppingCartType);

            //TODO: 239 Try to add ForeignKey
            //builder.HasOne(item => item.Customer)
            //    .WithMany(customer => customer.ShoppingCartItems)
            //    .HasForeignKey(item => item.CustomerId)
            //    .IsColumnRequired();

            //builder.HasOne(item => item.Product)
            //    .WithMany()
            //    .HasForeignKey(item => item.ProductId)
            //    .IsColumnRequired();
        }

        #endregion
    }
}