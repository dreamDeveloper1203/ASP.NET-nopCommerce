﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Customers
{
    /// <summary>
    /// Represents a reward points history mapping configuration
    /// </summary>
    public partial class RewardPointsHistoryMap : NopEntityTypeConfiguration<RewardPointsHistory>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<RewardPointsHistory> builder)
        {
            builder.ToTable(nameof(RewardPointsHistory));
            builder.HasKey(historyEntry => historyEntry.Id);

            builder.Property(historyEntry => historyEntry.UsedAmount).HasColumnType("decimal(18, 4)");

            base.Configure(builder);
        }

        #endregion
    }
}