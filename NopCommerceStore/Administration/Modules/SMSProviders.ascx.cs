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
using System.Collections;
using System.Configuration;
using System.Data;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Directory;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.BusinessLogic.Audit;
using System.IO;
using NopSolutions.NopCommerce.Froogle;
using NopSolutions.NopCommerce.PriceGrabber;
using NopSolutions.NopCommerce.Become;
using NopSolutions.NopCommerce.BusinessLogic.Messages;


namespace NopSolutions.NopCommerce.Web.Administration.Modules
{
    public partial class SMSProvidersControl : BaseNopAdministrationUserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                SelectTab(SMSProvidersTabs, TabId);
                FillDropDowns();
                BindData();
            }
        }

        private void BindData()
        {
            txtClickatellPhoneNumber.Text = SettingManager.GetSettingValue("Mobile.SMS.Clickatell.PhoneNumber");
            txtClickatellAPIId.Text = SettingManager.GetSettingValue("Mobile.SMS.Clickatell.APIID");
            txtClickatellUsername.Text = SettingManager.GetSettingValue("Mobile.SMS.Clickatell.Username");
            txtClickatellPassword.Text = SettingManager.GetSettingValue("Mobile.SMS.Clickatell.Password");
        }

        private void FillDropDowns()
        {
        }

        protected override void OnPreRender(EventArgs e)
        {
            BindJQuery();

            base.OnPreRender(e);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    ctrlClickatellProviderInfo.SaveInfo();

                    SettingManager.SetParam("Mobile.SMS.Clickatell.PhoneNumber", txtClickatellPhoneNumber.Text);
                    SettingManager.SetParam("Mobile.SMS.Clickatell.APIID", txtClickatellAPIId.Text);
                    SettingManager.SetParam("Mobile.SMS.Clickatell.Username", txtClickatellUsername.Text);
                    SettingManager.SetParam("Mobile.SMS.Clickatell.Password", txtClickatellPassword.Text);

                    CustomerActivityManager.InsertActivity("EditSMSProviders", GetLocaleResourceString("ActivityLog.EditSMSProviders"));

                    Response.Redirect(string.Format("SMSProviders.aspx?TabID={0}", GetActiveTabId(SMSProvidersTabs)));
                }
                catch (Exception exc)
                {
                    ProcessException(exc);
                }
            }
        }

        protected void BtnTestMessageSend_OnClick(object sender, EventArgs e)
        {
            try
            {
                int msgNum = SMSManager.SendSMS(txtTestMessageText.Text);
                ShowMessage(String.Format(GetLocaleResourceString("Admin.SMSProviders.TestMessage.Success"), msgNum));
            }
            catch (Exception exc)
            {
                ProcessException(exc);
            }
        }

        protected string TabId
        {
            get
            {
                return CommonHelper.QueryString("TabId");
            }
        }
    }
}