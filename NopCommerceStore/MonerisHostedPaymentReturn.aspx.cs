﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.SEO;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Moneris;
using NopSolutions.NopCommerce.BusinessLogic.Infrastructure;

namespace NopSolutions.NopCommerce.Web
{
    public partial class MonerisHostedPaymentReturn : BaseNopPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //comment this line to process return
            Response.Redirect(CommonHelper.GetStoreLocation());








            if(NopContext.Current.User == null)
            {
                string loginURL = SEOHelper.GetLoginPageUrl(true);
                Response.Redirect(loginURL);
            }

            if(!Page.IsPostBack)
            {
                CommonHelper.SetResponseNoCache(Response);

                string rspCode = CommonHelper.QueryString("response_code");
                if(String.IsNullOrEmpty(rspCode) || rspCode.ToUpperInvariant().Equals("NULL") || CommonHelper.QueryStringInt("response_code") >= 50)
                {
                    Response.Redirect(CommonHelper.GetStoreLocation());
                }

                Guid? guid = CommonHelper.QueryStringGuid("order_no");
                if(!guid.HasValue)
                {
                    Response.Redirect(CommonHelper.GetStoreLocation());
                }
                Order order = IoC.Resolve<IOrderService>().GetOrderByGuid(guid.Value);
                if(order == null)
                {
                    Response.Redirect(CommonHelper.GetStoreLocation());
                }
                if(NopContext.Current.User.CustomerId != order.CustomerId)
                {
                    Response.Redirect(CommonHelper.GetStoreLocation());
                }

                if(HostedPaymentSettings.AuthorizeOnly)
                {
                    if(IoC.Resolve<IOrderService>().CanMarkOrderAsAuthorized(order))
                    {
                        IoC.Resolve<IOrderService>().MarkAsAuthorized(order.OrderId);
                    }
                }
                else if(IoC.Resolve<IOrderService>().CanMarkOrderAsPaid(order))
                {
                    IoC.Resolve<IOrderService>().MarkOrderAsPaid(order.OrderId);
                }
                Response.Redirect("~/checkoutcompleted.aspx");
            }
        }

        public override bool AllowGuestNavigation
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this page is tracked by 'Online Customers' module
        /// </summary>
        public override bool TrackedByOnlineCustomersModule
        {
            get
            {
                return false;
            }
        }
    }
}
