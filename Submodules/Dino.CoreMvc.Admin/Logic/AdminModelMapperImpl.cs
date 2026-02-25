using System;
using System.Collections.Generic;
using Dino.Core.AdminBL.Settings;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dino.CoreMvc.Admin.Logic
{
    /// <summary>
    /// Implementation of IAdminModelMapper that wraps the static ModelMappingExtensions class
    /// </summary>
    public class AdminModelMapperImpl : IAdminModelMapper
    {
        private readonly IServiceProvider _serviceProvider;
        private ModelMappingContext _mappingContext;

        public AdminModelMapperImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private ModelMappingContext GetMappingContext(DbContext dbContext, object currentUserId = null)
        {
            return new ModelMappingContext
            {
                PluginRegistry = FieldTypePluginRegistry.GetInstance(_serviceProvider),
                CurrentUserId = currentUserId ?? 1, // Default to system user ID if not provided.
                DbContext = dbContext
            };
        }

        /// <inheritdoc />
        public object ToAdminModelFromTypes(object dbEntity, Type modelType, Type entityType, DbContext dbContext, object currentUserId = null)
        {
            // Call the static ModelMappingExtensions method
            var result = ModelMappingExtensions.ToAdminModelFromTypes(dbEntity, modelType, entityType, GetMappingContext(dbContext, currentUserId));
            
            // Return only the model part of the result
            return result.Model;
        }

        /// <inheritdoc />
        public object ToDbEntityFromTypes(object model, Type modelType, Type entityType, DbContext dbContext, object dbEntity = null, object currentUserId = null)
        {
            // Call the static ModelMappingExtensions method
            var result = ModelMappingExtensions.ToDbEntityFromTypes(model, modelType, entityType, GetMappingContext(dbContext, currentUserId), dbEntity);
            
            // Return only the entity part of the result
            return result.Entity;
        }

        public object ToAdminModelFromTypes(object dbEntity, Type modelType, Type entityType, object currentUserId = null)
        {
            // Get DbContext from service provider
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            
            // Delegate to the overload that has DbContext
            return ToAdminModelFromTypes(dbEntity, modelType, entityType, dbContext, currentUserId);
        }

        public object ToDbEntityFromTypes(object model, Type modelType, Type entityType, object dbEntity = null, object currentUserId = null)
        {
            // Get DbContext from service provider
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            
            // Delegate to the overload that has DbContext
            return ToDbEntityFromTypes(model, modelType, entityType, dbContext, dbEntity, currentUserId);
        }
    }
} 