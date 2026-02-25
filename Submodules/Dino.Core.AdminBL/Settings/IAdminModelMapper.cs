using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dino.Core.AdminBL.Settings
{
    /// <summary>
    /// Interface for mapping between database entities and admin models
    /// </summary>
    public interface IAdminModelMapper
    {
        /// <summary>
        /// Maps from a database entity to an admin model using runtime types
        /// </summary>
        /// <param name="dbEntity">The database entity to map from</param>
        /// <param name="modelType">The type of the admin model</param>
        /// <param name="entityType">The type of the database entity</param>
        /// <param name="currentUserId">The ID of the current user</param>
        /// <returns>The mapped admin model</returns>
        object ToAdminModelFromTypes(object dbEntity, Type modelType, Type entityType, DbContext dbContext = null, object currentUserId = null);

        /// <summary>
        /// Maps from an admin model to a database entity using runtime types
        /// </summary>
        /// <param name="model">The admin model to map from</param>
        /// <param name="modelType">The type of the admin model</param>
        /// <param name="entityType">The type of the database entity</param>
        /// <param name="dbEntity">Optional existing database entity to update</param>
        /// <param name="currentUserId">The ID of the current user</param>
        /// <returns>The mapped database entity</returns>
        object ToDbEntityFromTypes(object model, Type modelType, Type entityType, DbContext dbContext, object dbEntity = null, object currentUserId = null);
    }
} 