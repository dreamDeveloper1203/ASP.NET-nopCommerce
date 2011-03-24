﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using Autofac;

namespace Nop.Web.Framework.Mvc.Routes
{
    public class RoutePublisher : IRoutePublisher
    {
        private readonly RouteCollection _routeCollection;
        //private readonly ShellSettings _shellSettings;
        private readonly ILifetimeScope _shellLifetimeScope;
        //private readonly IRunningShellTable _runningShellTable;

        public RoutePublisher(
            RouteCollection routeCollection,
            ILifetimeScope shellLifetimeScope)
        {
            _routeCollection = routeCollection;
            _shellLifetimeScope = shellLifetimeScope;
        }

        public void Publish(IEnumerable<RouteDescriptor> routes)
        {
            var routesArray = routes.OrderByDescending(r => r.Priority).ToArray();

            // this is not called often, but is intended to surface problems before
            // the actual collection is modified
            var preloading = new RouteCollection();
            foreach (var route in routesArray)
                preloading.Add(route.Name, route.Route);

            using (_routeCollection.GetWriteLock())
            {
                foreach (var routeDescriptor in routesArray)
                {
                    _routeCollection.Add(routeDescriptor.Name, routeDescriptor.Route);
                }
            }
        }
    }
}
