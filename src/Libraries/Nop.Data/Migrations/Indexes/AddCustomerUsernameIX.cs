﻿using FluentMigrator;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.Indexes
{
    [Migration(637123449689037682)]
    public class AddCustomerUsernameIX : AutoReversingMigration
    {
        #region Methods          

        public override void Up()
        {
            this.AddIndex("IX_Customer_Username", nameof(Customer), i => i.Ascending(), nameof(Customer.Username));
        }

        #endregion
    }
}