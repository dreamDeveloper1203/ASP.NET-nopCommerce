
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using System.IO;
using System.Xml.Serialization;
using Nop.Core.Domain.Security;
using Nop.Services.Localization;
using Nop.Core.Infrastructure;

namespace Nop.Services.Customers
{
    public static class CustomerExtentions
    {
        /// <summary>
        /// Gets a value indicating whether customer is in a certain customer role
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="customerRoleSystemName">Customer role system name</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public static bool IsInCustomerRole(this Customer customer,
            string customerRoleSystemName, bool onlyActiveCustomerRoles = true)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            if (String.IsNullOrEmpty(customerRoleSystemName))
                throw new ArgumentNullException("customerRoleSystemName");

            var result = customer.CustomerRoles
                .Where(cr => !onlyActiveCustomerRoles || cr.Active)
                .Where(cr => cr.SystemName == customerRoleSystemName)
                .FirstOrDefault() != null;
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customer is administrator
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public static bool IsAdmin(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Administrators, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public static bool IsForumModerator(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.ForumModerators, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is registered
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public static bool IsRegistered(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Registered, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is guest
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
        /// <returns>Result</returns>
        public static bool IsGuest(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Guests, onlyActiveCustomerRoles);
        }

        public static T GetAttribute<T>(this Customer customer,
            string key)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var customerAttribute = customer.CustomerAttributes.FirstOrDefault(ca => ca.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            if (customerAttribute == null)
                return default(T);

            if (String.IsNullOrEmpty(customerAttribute.Value))
            {
                //empty attribute
                return default(T);
            }
            else
            {
                if (!TypeDescriptor.GetConverter(typeof(T)).CanConvertFrom(typeof(string)))
                    throw new NopException("Not supported customer attribute type");
                var attributeValue = (T)(TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(customerAttribute.Value));
                return attributeValue;

                //use the code below in order to support all serializable types (for example, ShippingOption)
                //or use custom TypeConverters like it's implemented for ISettings
                //using (var tr = new StringReader(customerAttribute.Value))
                //{
                //    var xmlS = new XmlSerializer(typeof(T));
                //    var attributeValue = (T)xmlS.Deserialize(tr);
                //    return attributeValue;
                //}

            }
        }

        public static User GetDefaultUserAccount(this Customer customer, bool onlyActiveUser = true)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");
            
            var user = customer.AssociatedUsers
                .Where(u => !onlyActiveUser || (u.IsApproved && !u.IsLockedOut))
                .OrderByDescending(u => u.CreatedOnUtc)
                .FirstOrDefault();
            return user;
        }

        public static string GetDefaultUserAccountEmail(this Customer customer, bool onlyActiveUser = true)
        {
            var user = GetDefaultUserAccount(customer, onlyActiveUser);
            return user != null ? user.Email : null;
        }

        public static string GetDefaultUserAccountUsername(this Customer customer, bool onlyActiveUser = true)
        {
            var user = GetDefaultUserAccount(customer, onlyActiveUser);
            return user != null ? user.Username : null;
        }

        public static string GetFullName(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");
            var firstName = customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName);
            var lastName = customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName);

            string fullName = "";
            if (!String.IsNullOrWhiteSpace(firstName) && !String.IsNullOrWhiteSpace(lastName))
                fullName = string.Format("{0} {1}", firstName, lastName);
            else
            {
                if (!String.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!String.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }
            return fullName;
        }

        /// <summary>
        /// Formats the customer name
        /// </summary>
        /// <param name="customer">Source</param>
        /// <returns>Formatted text</returns>
        public static string FormatUserName(this Customer customer)
        {
            return FormatUserName(customer, false);
        }

        /// <summary>
        /// Formats the customer name
        /// </summary>
        /// <param name="customer">Source</param>
        /// <param name="stripTooLong">Strip too long customer name</param>
        /// <returns>Formatted text</returns>
        public static string FormatUserName(this Customer customer, bool stripTooLong)
        {
            if (customer == null)
                return string.Empty;

            if (customer.IsGuest())
            {
                return EngineContext.Current.Resolve<ILocalizationService>().GetResource("Customer.Guest");
            }

            string result = string.Empty;
            switch (EngineContext.Current.Resolve<CustomerSettings>().CustomerNameFormat)
            {
                case CustomerNameFormat.ShowEmails:
                    result = customer.GetDefaultUserAccountEmail();
                    break;
                case CustomerNameFormat.ShowFullNames:
                    result = customer.GetFullName();
                    break;
                case CustomerNameFormat.ShowUsernames:
                    result = customer.GetDefaultUserAccountUsername();
                    break;
                default:
                    break;
            }

            if (stripTooLong)
            {
                int maxLength = 0; // EngineContext.Current.Resolve<CustomerSettings>().FormatNameMaxLength;
                if (maxLength > 0 && result.Length > maxLength)
                {
                    result = result.Substring(0, maxLength);
                }
            }

            return result;
        }

    }
}
