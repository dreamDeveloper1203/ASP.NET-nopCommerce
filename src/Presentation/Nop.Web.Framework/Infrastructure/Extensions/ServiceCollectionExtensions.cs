﻿using System;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Web.Framework.FluentValidation;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Framework.Infrastructure.Extensions
{
    /// <summary>
    /// Represents extensions of IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services to the application and configure service provider
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        /// <returns></returns>
        public static IServiceProvider ConfigureApplicationServices(this IServiceCollection services, IConfigurationRoot configuration)
        {
            //create engine
            var engine = EngineContext.Create();

            //then initialize
            var hostingEnvironment = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>();
            engine.Initialize(hostingEnvironment);

            //and configure it
            engine.ConfigureServices(services, configuration);

            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                //database is already installed, so start scheduled tasks
                //TODO uncomment schedule tasks
                //TaskManager.Instance.Initialize();
                //TaskManager.Instance.Start();

                try
                {
                    //and log application start
                    EngineContext.Current.Resolve<ILogger>().Information("Application started", null, null);
                }
                catch { }
            }

            return engine.ServiceProvider;
        }

        /// <summary>
        /// Adds services required for application session state
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddHttpSession(this IServiceCollection services)
        {
            services.AddSession(
#if NET451
                options => 
                //Determines the cookie name used to persist the session ID. Defaults to Microsoft.AspNetCore.Session.SessionDefaults.CookieName.
                options.CookieName
                
                //Determines the domain used to create the cookie. Is not provided by default.
                options.CookieDomain
                
                //Determines the path used to create the cookie. Defaults to Microsoft.AspNetCore.Session.SessionDefaults.CookiePath.
                options.CookiePath
                
                //Determines if the browser should allow the cookie to be accessed by client-side JavaScript. The default is true, 
                //which means the cookie will only be passed to HTTP requests and is not made available to script on the page.
                options.CookieHttpOnly
                
                //Determines if the cookie should only be transmitted on HTTPS requests.
                options.CookieSecure
                
                //The IdleTimeout indicates how long the session can be idle before its contents are abandoned. 
                //Each session access resets the timeout. Note this only applies to the content of the session, not the cookie.
                options.IdleTimeout
#endif
            );
        }

        /// <summary>
        /// Add and configure MVC for the application
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <returns>A builder for configuring MVC services</returns>
        public static IMvcBuilder AddNopMvc(this IServiceCollection services)
        {
            //add basic MVC feature
            var mvcBuilder = services.AddMvc();

            //add custom display metadata provider
            mvcBuilder.AddMvcOptions(options => options.ModelMetadataDetailsProviders.Add(new NopMetadataProvider()));

            //add custom model binder provider (to the top of the provider list)
            mvcBuilder.AddMvcOptions(options => options.ModelBinderProviders.Insert(0, new NopModelBinderProvider()));

#if NET451
            //whether database is already installed
            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                //add themeable view engine
                mvcBuilder.AddViewOptions(options =>
                {
                    options.ViewEngines.Clear();
                    options.ViewEngines.Add(new ThemeableRazorViewEngine());
                });
            }
#endif
            //add global exception filter
            mvcBuilder.AddMvcOptions(options => options.Filters.Add(new ExceptionFilter()));

            //add fluent validation
            mvcBuilder.AddFluentValidation(configuration => configuration.ValidatorFactoryType = typeof(NopValidatorFactory));

            return mvcBuilder;
        }
    }
}
