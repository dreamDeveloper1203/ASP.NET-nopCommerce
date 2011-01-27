//------------------------------------------------------------------------------
// The contents of this file are subject to the nopCommerce Public License Version 1.0 ("License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  http://www.nopCommerce.com/License.aspx. 
// 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
// See the License for the specific language governing rights and limitations under the License.
// 
// The Original Code is nopCommerce.
// The Initial Developer of the Original Code is NopSolutions.
// All Rights Reserved.
// 
// Contributor(s): _______. 
//------------------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using Nop.Core.Domain.Catalog;


namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductVariantAttributeValueMap : EntityTypeConfiguration<ProductVariantAttributeValue>
    {
        public ProductVariantAttributeValueMap()
        {
            this.ToTable("ProductVariantAttributeValue");
            this.HasKey(pvav => pvav.Id);
            this.Property(pvav => pvav.Name).HasMaxLength(400);

            this.Property(pvav => pvav.PriceAdjustment).HasPrecision(18, 4);
            this.Property(pvav => pvav.WeightAdjustment).HasPrecision(18, 4);

            this.HasRequired(pvav => pvav.ProductVariantAttribute)
                .WithMany(pva => pva.ProductVariantAttributeValues)
                .HasForeignKey(pvav => pvav.ProductVariantAttributeId);
        }
    }
}