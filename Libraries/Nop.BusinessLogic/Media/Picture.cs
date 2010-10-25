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

using System;
using System.Collections.Generic;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Products;
using NopSolutions.NopCommerce.BusinessLogic.IoC;


namespace NopSolutions.NopCommerce.BusinessLogic.Media
{
    /// <summary>
    /// Represents a picture
    /// </summary>
    public partial class Picture : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the Picture class
        /// </summary>
        public Picture()
        {
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the picture identifier
        /// </summary>
        public int PictureId { get; set; }

        /// <summary>
        /// Gets or sets the picture binary
        /// </summary>
        public byte[] PictureBinary { get; set; }

        /// <summary>
        /// Gets or sets the picture mime type
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the picture is new
        /// </summary>
        public bool IsNew { get; set; }

        #endregion

        #region Methods properties

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <param name="fromDB">Load from database; otherwise, from file system</param>
        /// <returns>Picture binary</returns>
        public byte[] LoadPictureBinary(bool fromDB)
        {
            byte[] result = null;
            if (fromDB)
            {
                result = this.PictureBinary;
            }
            else
            {
                result = IoCFactory.Resolve<IPictureManager>().LoadPictureFromFile(this.PictureId, this.MimeType);
            }
            return result;
        }

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <returns>Picture binary</returns>
        public byte[] LoadPictureBinary()
        {
            return LoadPictureBinary(IoCFactory.Resolve<IPictureManager>().StoreInDB);
        }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets the picture
        /// </summary>
        public virtual ICollection<ProductPicture> NpProductPictures { get; set; }

        #endregion
    }
}
