﻿using System.Data.Entity.ModelConfiguration;
using Nop.Core.Domain.Tax;

namespace Nop.Data.Mapping.Tax
{
    public class TaxCategoryMap : EntityTypeConfiguration<TaxCategory>
    {
        public TaxCategoryMap()
        {
            this.ToTable("TaxCategory");
            this.HasKey(p => p.Id);
        }
    }
}
