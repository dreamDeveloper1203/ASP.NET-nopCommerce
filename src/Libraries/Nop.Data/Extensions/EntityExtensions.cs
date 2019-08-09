﻿using System;
using Nop.Core;
using Nop.Core.Caching;

namespace Nop.Data.Extensions
{
    /// <summary>
    /// Represents extensions
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Check whether an entity is proxy
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Result</returns>
        [Obsolete("Will be removed after branch merge issue-239-ef-performance. The recommended alternative is IsProxy(this Type entity)")]
        private static bool IsProxy(this BaseEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //in EF 6 we could use ObjectContext.GetObjectType. Now it's not available. Here is a workaround:

            var type = entity.GetType();
            //e.g. "CustomerProxy" will be derived from "Customer". And "Customer" is derived from BaseEntity
            return type.BaseType != null && type.BaseType.BaseType != null && type.BaseType.BaseType == typeof(BaseEntity);
        }

        /// <summary>
        /// Get unproxied entity type
        /// </summary>
        /// <remarks> If your Entity Framework context is proxy-enabled, 
        /// the runtime will create a proxy instance of your entities, 
        /// i.e. a dynamically generated class which inherits from your entity class 
        /// and overrides its virtual properties by inserting specific code useful for example 
        /// for tracking changes and lazy loading.
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        [Obsolete("Will be removed after branch merge issue-239-ef-performance. The recommended alternative is GetUnproxiedEntityType(this Type entity)")]
        public static Type GetUnproxiedEntityType(this BaseEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Type type = null;
            //cachable entity (get the base entity type)
            if (entity is IEntityForCaching)
                type = ((IEntityForCaching)entity).GetType().BaseType;
            //EF proxy
            else if (entity.IsProxy())
                type = entity.GetType().BaseType;
            //not proxied entity
            else
                type = entity.GetType();

            if (type == null)
                throw new Exception("Original entity type cannot be loaded");

            return type;
        }

        /// <summary>
        /// Check whether an entity is proxy
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Result</returns>
        private static bool IsProxy(this Type entity)
        {
            //in EF 6 we could use ObjectContext.GetObjectType. Now it's not available. Here is a workaround:
            //e.g. "CustomerProxy" will be derived from "Customer". And "Customer" is derived from BaseEntity
            return entity.BaseType != null && entity.BaseType.BaseType != null && entity.BaseType.BaseType == typeof(BaseEntity);
        }

        /// <summary>
        /// Get unproxied entity type
        /// </summary>
        /// <remarks> If your Entity Framework context is proxy-enabled, 
        /// the runtime will create a proxy instance of your entities, 
        /// i.e. a dynamically generated class which inherits from your entity class 
        /// and overrides its virtual properties by inserting specific code useful for example 
        /// for tracking changes and lazy loading.
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Type GetUnproxiedEntityType(this Type entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            switch (entity)
            {
                //cachable entity (get the base entity type)
                case Type cachingType when typeof(IEntityForCaching).IsAssignableFrom(cachingType):
                    return cachingType.BaseType;
                //EF proxy
                case Type proxy when proxy.IsProxy():
                    return entity.BaseType;
                //not proxied entity
                default:
                    return entity;
            }
        }
    }
}