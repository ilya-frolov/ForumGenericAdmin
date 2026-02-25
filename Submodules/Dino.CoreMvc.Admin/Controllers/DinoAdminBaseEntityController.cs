using System.Reflection;
using AutoMapper;
using Dino.CoreMvc.Admin.AutoMapper;
using System.Linq.Expressions;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.Core.AdminBL.Cache;
using System.Threading.Tasks;
using Dino.CoreMvc.Admin.Logic;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Dino.Common.AzureExtensions.Files.Uploaders;
using Dino.Infra.Reflection;
using Dino.CoreMvc.Admin.Models.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Dino.Infra.Files.Uploaders;
using System.Collections.Generic;
using System.Linq;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Dino.CoreMvc.Admin.Helpers;

namespace Dino.CoreMvc.Admin.Controllers
{
    public abstract partial class DinoAdminBaseEntityController<TModel, TEFEntity, TIdType> : DinoAdminBaseController
        where TModel : BaseAdminModel, new()
        where TEFEntity : class, new()
        where TIdType : notnull
    {
        protected DinoAdminBaseEntityController(string id) : base(id)
        {
        }

        protected override bool IsGeneric()
        {
            return true;
        }


        #region List Actions

        /// LOOK INSIDE THE PARTIAL LIST CLASS :)

        #endregion

        #region Edit Actions

        /// LOOK INSIDE THE PARTIAL EDIT CLASS :)

        #endregion

        #region GetEntitiesByIds

        protected IQueryable<TEFEntity> GetEntitiesByIds(List<TIdType> entityIds)
        {
            var query = DbContext.Set<TEFEntity>().AsQueryable();

            // Create the lambda expression to filter the entities by id
            var parameter = Expression.Parameter(typeof(TEFEntity), "x");
            var property = Expression.Property(parameter, "Id");
            var containsMethod = typeof(List<TIdType>).GetMethod("Contains");
            var entityIdsExpr = Expression.Constant(entityIds);
            var containsCall = Expression.Call(entityIdsExpr, containsMethod, property);
            var lambda = Expression.Lambda<Func<TEFEntity, bool>>(containsCall, parameter);

            query = query.Where(lambda);

            return query;
        }

        #endregion

        #region GetModelPropertyWithAttribute

        protected PropertyInfo? GetModelPropertyWithAttribute<TAttribute>() where TAttribute : Attribute
        {
            return typeof(TModel).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<TAttribute>() != null);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// This is used to get the type of the model that is used in the controller.
        /// You can override this for inherited models to return the correct type, especially for the form structure methods (on edit).
        /// </summary>
        /// <param name="id">The id of the entity (if editing).</param>
        /// <param name="model">The admin model of the entity (if editing).</param>
        /// <param name="entity">The database entity of the entity (if editing).</param>
        /// <returns>The type of the model.</returns>
        public virtual Type GetAdminModelType(string id, TModel model, TEFEntity entity)
        {
            return typeof(TModel);
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Determines which cache models are potentially affected by changes to this controller's TEFEntity type.
        /// Default implementation finds all cache models whose DbModelType matches TEFEntity.
        /// Override in derived controllers for more specific mapping.
        /// </summary>
        /// <returns>An enumerable of relevant cache model types.</returns>
        protected virtual IEnumerable<Type> GetRelatedCacheModelTypes()
        {
            var dbEntityType = typeof(TEFEntity);
            return CacheUtils.GetAllCacheModelTypes()
                             .Where(cacheType => CacheUtils.GetCacheAttribute(cacheType)?.DbModelType == dbEntityType);
        }

        protected virtual async Task OnEntityCreatedForCache(TModel adminModel, TEFEntity efModel, object id)
        {
            if (DinoCacheManager == null || efModel == null || id.Equals(default(TIdType))) return;

            // Use the new method to get relevant types
            var relatedCacheTypes = GetRelatedCacheModelTypes();

            foreach (var cacheType in relatedCacheTypes)
            {
                var cacheAttribute = CacheUtils.GetCacheAttribute(cacheType);
                if (cacheAttribute?.CacheOnCreate == true)
                {
                    Logger?.LogDebug("Updating cache (on create) for {CacheTypeName} ID {EntityId} due to change in {EFEntityTypeName}",
                        cacheType.Name, id, typeof(TEFEntity).Name);

                    await DinoCacheManager.Update(cacheType, id, efModel);
                }
            }
        }

        protected virtual async Task OnEntityUpdatedForCache(TModel adminModel, TEFEntity efModel, object id)
        {
            if (DinoCacheManager == null || efModel == null || id.Equals(default(TIdType))) return;

            var relatedCacheTypes = GetRelatedCacheModelTypes();

            foreach (var cacheType in relatedCacheTypes)
            {
                var cacheAttribute = CacheUtils.GetCacheAttribute(cacheType);
                if (cacheAttribute?.UpdateOnEdit == true)
                {
                     Logger?.LogDebug("Updating cache (on update) for {CacheTypeName} ID {EntityId} due to change in {EFEntityTypeName}",
                        cacheType.Name, id, typeof(TEFEntity).Name);

                    await DinoCacheManager.Update(cacheType, id, efModel);
                }
            }
        }

        protected virtual async Task OnEntityDeletedForCache(object id)
        {
            if (DinoCacheManager == null || id.Equals(default(TIdType))) return;

            var relatedCacheTypes = GetRelatedCacheModelTypes();

            foreach (var cacheType in relatedCacheTypes)
            {
                var cacheAttribute = CacheUtils.GetCacheAttribute(cacheType);
                if (cacheAttribute?.RemoveOnDelete == true)
                {
                    // Logger?.LogDebug("Removing from cache {CacheTypeName} ID {EntityId} due to deletion of {EFEntityTypeName}",
                    //    cacheType.Name, id, typeof(TEFEntity).Name);

                    // Call generic Remove method with TIdType
                    DinoCacheManager.Remove(cacheType, id); // Pass TIdType directly
                }
            }
        }

        protected virtual async Task OnSortChangedForCache(IEnumerable<TIdType> sortedIds)
        {
            if (DinoCacheManager == null || sortedIds == null) return;

            var relatedCacheTypes = GetRelatedCacheModelTypes();
            // Remove Cast<object>() - pass the strongly typed IEnumerable<TIdType>
            // var objectIds = sortedIds.Cast<object>().ToList();

            if (!sortedIds.Any()) return; // Nothing to update

            foreach (var cacheType in relatedCacheTypes)
            {
                var cacheAttribute = CacheUtils.GetCacheAttribute(cacheType);
                if (cacheAttribute?.ReloadOnSort == true)
                {
                    // Logger?.LogDebug("Updating cache (on sort) for {CacheTypeName} for specific IDs due to sort change in {EFEntityTypeName}",
                    //    cacheType.Name, typeof(TEFEntity).Name);

                    // Call generic UpdateCacheForItems with TIdType
                    await DinoCacheManager.UpdateCacheForItems<TIdType>(cacheType, sortedIds);
                }
            }
        }

        #endregion

        #region Permissions


        protected async Task<bool> CheckPermission(PermissionType permission, string? refId = null, bool checkListDef = true)
        {
            var permissionAttr = GetType().GetCustomAttribute<AdminPermissionAttribute>();
            return await this.CheckPermission(permissionAttr, permission, refId, checkListDef);
        }

        protected async Task<bool> CheckPermission(AdminPermissionAttribute permissionAttr, PermissionType permission, string? refId, bool checkListDef = true)
        {
            var currentUserRoleIdentifier = GetCurrentAdminUserRoleIdentifier();
            var currentUserRoleType = GetCurrentAdminUserRoleType();

            var permissionAllowed = PermissionHelper.CheckPermission(permissionAttr, currentUserRoleIdentifier, currentUserRoleType, permission);

            if (checkListDef)
            {
                var listDef = await CreateListDef(null);
                if (listDef != null)
                {
                    switch (permission)
                    {
                        case PermissionType.Add:
                            permissionAllowed &= listDef.AllowAdd;
                            break;
                        case PermissionType.Edit:
                            permissionAllowed &= listDef.AllowEdit;
                            break;
                        case PermissionType.Delete:
                            permissionAllowed &= listDef.AllowDelete;
                            break;
                        case PermissionType.Archive:
                            permissionAllowed &= listDef.ShowArchive;
                            break;
                        case PermissionType.Import:
                            permissionAllowed &= listDef.AllowExcelImport;
                            break;
                        case PermissionType.Export:
                            permissionAllowed &= (listDef.AllowedExportFormats != ExportFormat.None);
                            break;
                    }
                }
            }

            return permissionAllowed;
        }

        #endregion
    }
}
