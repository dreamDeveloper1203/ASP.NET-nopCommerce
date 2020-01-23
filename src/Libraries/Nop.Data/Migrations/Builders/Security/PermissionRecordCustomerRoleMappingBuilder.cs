﻿using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.Builders
{
    public partial class PermissionRecordCustomerRoleMappingBuilder : BaseEntityBuilder<PermissionRecordCustomerRoleMapping>
    {
        #region Methods

        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(PermissionRecordCustomerRoleMapping.PermissionRecordId))
                    .AsInt32()
                    .PrimaryKey()
                    .ForeignKey<PermissionRecord>()
                .WithColumn(nameof(PermissionRecordCustomerRoleMapping.CustomerRoleId))
                    .AsInt32()
                    .PrimaryKey()
                    .ForeignKey<CustomerRole>();
        }

        #endregion
    }
}