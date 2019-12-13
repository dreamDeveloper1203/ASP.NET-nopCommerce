﻿using FluentMigrator;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Shipping;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.Catalog
{
    [Migration(637097656165419187)]
    public class AddStockQuantityHistoryWarehouseFK : AutoReversingMigration
    {
        #region Methods

        public override void Up()
        {
            this.AddForeignKey(nameof(StockQuantityHistory)
                , nameof(StockQuantityHistory.WarehouseId)
                , nameof(Warehouse)
                , nameof(Warehouse.Id));
        }

        #endregion
    }
}