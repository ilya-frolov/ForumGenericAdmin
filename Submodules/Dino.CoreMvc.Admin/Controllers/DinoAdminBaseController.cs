using Dino.Core.AdminBL.Contracts;
using Dino.Core.AdminBL.Data;
using Dino.CoreMvc.Admin.Contracts;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.Logic;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.Mvc.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dino.Core.AdminBL.Cache;
using System.Linq;
using System.Security.Claims;
using System.Reflection; // Required for GetCustomAttribute
using Dino.CoreMvc.Admin.Attributes.Permissions; // Required for AdminPermissionAttribute and PermissionType
using Dino.CoreMvc.Admin.Helpers;
using Dino.CoreMvc.Admin.Models; // Required for PermissionHelper

namespace Dino.CoreMvc.Admin.Controllers
{
    [Area("Admin")]
    public abstract class DinoAdminBaseController : DinoController
    {
        private IOptions<AdminConfig> _adminConfig;

        protected IOptions<AdminConfig> AdminConfig =>
            _adminConfig ??= HttpContext?.RequestServices.GetService<IOptions<AdminConfig>>();

        private IOptions<BaseApiConfig> _baseApiConfig;

        protected IOptions<BaseApiConfig> ApiConfig =>
            _baseApiConfig ??= HttpContext?.RequestServices.GetService<IOptions<BaseApiConfig>>();

        private IOptions<BaseBlConfig> _baseBlConfig;

        protected IOptions<BaseBlConfig> BlConfig =>
            _baseBlConfig ??= HttpContext?.RequestServices.GetService<IOptions<BaseBlConfig>>();

        private BaseAdminDbContext _dbContext;

        protected BaseAdminDbContext DbContext
        {
            get
            {
                if (_dbContext == null && HttpContext != null)
                {
                    _dbContext = (BaseAdminDbContext)HttpContext.RequestServices.GetService<DbContext>();
                }

                return _dbContext;
            }
            private set => _dbContext = value;
        }

        private ModelMappingContext _mappingContext;

        protected ModelMappingContext MappingContext
        {
            get
            {
                if (_mappingContext == null && HttpContext != null)
                {
                    var mappingContext = new ModelMappingContext
                    {
                        CurrentUserId = GetCurrentAdminUserId(),
                        PluginRegistry = FieldTypePluginRegistry.GetInstance(HttpContext?.RequestServices),
                        DbContext = DbContext
                    };

                    _mappingContext = mappingContext;
                }

                return _mappingContext ??
                       throw new InvalidOperationException(
                           "Model Mapping Context is not initialized. HttpContext may not be available.");
            }
        }

        private IDinoCacheManager _dinoCacheManager;

        protected IDinoCacheManager DinoCacheManager
        {
            get
            {
                if (_dinoCacheManager == null && HttpContext != null)
                {
                    _dinoCacheManager = HttpContext.RequestServices.GetService<IDinoCacheManager>();
                }

                return _dinoCacheManager;
            }
        }

        private ILogger _logger;

        protected ILogger Logger
        {
            get
            {
                if (_logger == null && HttpContext != null)
                {
                    var loggerType = typeof(ILogger<>).MakeGenericType(this.GetType());
                    _logger = (ILogger)HttpContext.RequestServices.GetService(loggerType);
                }

                return _logger ??
                       throw new InvalidOperationException(
                           "Logger is not initialized. HttpContext may not be available.");
            }
        }

        public string Id { get; private set; }

        protected DinoAdminBaseController(string id)
        {
            Id = id;
        }

        public async Task<AdminSegment> GetAdminSegment()
        {
            var segment = await CreateAdminSegment();
            segment.General.Id = Id;
            segment.General.IsGeneric = IsGeneric();
            segment.Navigation.ControllerName = GetType().Name.Replace("Controller", string.Empty);

            return segment;
        }

        protected abstract bool IsGeneric();

        protected abstract Task<AdminSegment> CreateAdminSegment();

        protected int GetCurrentAdminUserId()
        {
            if (HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim =
                    HttpContext.User.Claims.FirstOrDefault(c =>
                        c.Type == "Id"); // "Id" is the claim type used for user's primary key
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }

            // Consider the appropriate default or error handling if ID is not found or user not authenticated.
            // Returning 0 or -1 might be a common default if a system/anonymous ID is sometimes needed.
            // For critical operations, throwing an exception might be better if a valid user ID is always expected.
            _logger?.LogWarning(
                "Could not determine CurrentUserId. User might not be authenticated or 'Id' claim is missing. Defaulting to 0.");
            return 0; // Defaulting to 0 as a fallback; adjust if a different default or error is preferred.
        }

        protected int GetCurrentAdminUserRoleIdentifier()
        {
            if (HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var roleIdentifierClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserRoleIdentifier");
                if (roleIdentifierClaim != null && int.TryParse(roleIdentifierClaim.Value, out int roleIdentifier))
                {
                    return roleIdentifier;
                }
            }

            return -1; // Sentinel value indicating not found, not authenticated, or not applicable
        }

        protected short GetCurrentAdminUserRoleType()
        {
            if (HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var roleTypeClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserRoleType");
                if (roleTypeClaim != null && short.TryParse(roleTypeClaim.Value, out short roleType))
                {
                    return roleType;
                }
            }

            return -1; // Sentinel value indicating not found, not authenticated, or not applicable
        }
    }
}