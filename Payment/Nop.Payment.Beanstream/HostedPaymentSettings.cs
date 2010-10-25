﻿//------------------------------------------------------------------------------
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
// Contributor(s): 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.BusinessLogic.IoC;

namespace NopSolutions.NopCommerce.Payment.Methods.Beanstream
{
    public class HostedPaymentSettings
    {
        #region Properties
        public static string GatewayUrl
        {
            get
            {
                return IoCFactory.Resolve<ISettingManager>().GetSettingValue("PaymentMethod.Beanstream.HostedPayment.GatewayUrl", "https://www.beanstream.com/scripts/payment/payment.asp");
            }
            set
            {
                IoCFactory.Resolve<ISettingManager>().SetParam("PaymentMethod.Beanstream.HostedPayment.GatewayUrl", value);
            }
        }

        public static string MerchantId
        {
            get
            {
                return IoCFactory.Resolve<ISettingManager>().GetSettingValue("PaymentMethod.Beanstream.HostedPayment.MerchantID");
            }
            set
            {
                IoCFactory.Resolve<ISettingManager>().SetParam("PaymentMethod.Beanstream.HostedPayment.MerchantID", value);
            }
        }

        public static decimal AdditionalFee
        {
            get
            {
                return IoCFactory.Resolve<ISettingManager>().GetSettingValueDecimalNative("PaymentMethod.Beanstream.HostedPayment.AdditionalFee");
            }
            set
            {
                IoCFactory.Resolve<ISettingManager>().SetParamNative("PaymentMethod.Beanstream.HostedPayment.AdditionalFee", value);
            }
        }
        #endregion
    }
}
