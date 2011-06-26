﻿using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Framework
{
    public class NopResourceDisplayName : System.ComponentModel.DisplayNameAttribute, IModelAttribute
    {
        public NopResourceDisplayName(string resourceKey)
            : base(GetResource(resourceKey))
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; set; }

        private static string GetResource(string resourceKey)
        {
            var value = EngineContext.Current.Resolve<ILocalizationService>().GetResource(resourceKey,
                                                                                     EngineContext.Current.Resolve
                                                                                         <IWorkContext>().
                                                                                         WorkingLanguage.Id, true,
                                                                                     resourceKey);
            return value;
        }


        public string Name
        {
            get { return "NopResourceDisplayName"; }
        }
    }
}
