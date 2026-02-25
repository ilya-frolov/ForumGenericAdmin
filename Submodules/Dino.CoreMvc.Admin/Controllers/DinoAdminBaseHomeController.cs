using System.ComponentModel;
using System.Reflection;
using Dino.Common.Helpers;
using Dino.Core.AdminBL.Data;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.Mvc.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using System.Collections.Generic;
using System.Linq;
using Dino.CoreMvc.Admin.Helpers;
using System.Security;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Dino.CoreMvc.Admin.Controllers
{
    public abstract class DinoAdminBaseHomeController() : DinoAdminBaseController("Home")
    {
        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            return null;
        }

        protected override bool IsGeneric()
        {
            return false;
        }

        [AllowAnonymous]
        public virtual async Task<JsonResult> GetInitData()
        {
            var data = new InitDataModel
            {
                AllowHebrew = AdminConfig.Value.AllowHebrew,
                AllowEnglish = AdminConfig.Value.AllowEnglish,
                NewServerUrl = AdminConfig.Value.NewServerUrl
            };

            return CreateJsonResponse(data);
        }

        public virtual async Task<JsonResult> GetHomeData()
        {
            var adminSegments = await GetAllSegmentsInAssembly();
            var settings = await GetAllSettingsInAssembly();

            var homeData = new AdminHomeData
            {
                Segments = adminSegments,
                Settings = settings,
                //ShowDashboard = CanShowDashboard(),
                //ShowGlobalStatistics = CanShowGlobalStatistics(),
                UserName = await GetUserName()
            };

            return CreateJsonResponse(homeData);
        }

        protected virtual async Task<string> GetUserName()
        {
            // TODO
            return "Admin";
        }

        private async Task<List<AdminSegment>> GetAllSegmentsInAssembly()
        {
            var assembly = Assembly.GetAssembly(GetType());
            var baseControllerType = typeof(DinoAdminBaseController);
            var homeControllerType = typeof(DinoAdminBaseHomeController);

            var allControllerTypes = assembly.GetTypes()
                .Where(p => baseControllerType.IsAssignableFrom(p) &&
                            !homeControllerType.IsAssignableFrom(p) &&
                            !p.IsInterface &&
                            !p.IsAbstract)
                .OrderBy(t => {
                    // Get priority for sorting, even for non-permitted controllers
                    try {
                        var instance = (DinoAdminBaseController)Activator.CreateInstance(t);
                        var segment = instance.GetAdminSegment().Result;
                        return segment?.General?.Priority ?? 999;
                    } catch {
                        return 999;
                    }
                });

            var currentUserRoleIdentifier = GetCurrentAdminUserRoleIdentifier();
            var currentUserRoleType = GetCurrentAdminUserRoleType();
            string currentHeader = null; // Track header that should apply to subsequent permitted segments

            var permittedSegments = new List<AdminSegment>();

            foreach (var controllerType in allControllerTypes)
            {
                var permissionAttr = controllerType.GetCustomAttribute<AdminPermissionAttribute>();
                var hasPermission = PermissionHelper.CheckPermission(permissionAttr, currentUserRoleIdentifier, currentUserRoleType, PermissionType.View);

                AdminSegment segment = null;
                string segmentMenuHeader = null;

                // CRITICAL: Get segment info from ALL controllers (even non-permitted ones)
                // to extract MenuHeader information that affects subsequent segments
                try {
                    var instance = (DinoAdminBaseController)Activator.CreateInstance(controllerType);
                    var tempSegment = await instance.GetAdminSegment();
                    segmentMenuHeader = tempSegment?.General?.MenuHeader;

                    // Only keep the full segment if user has permission to view it
                    if (hasPermission)
                    {
                        segment = tempSegment;
                    }
                } catch {
                    // Ignore errors for controllers that can't be instantiated
                    continue;
                }

                // KEY LOGIC: Update current header whenever ANY controller defines a MenuHeader
                // This ensures headers "flow through" even if the defining controller is not permitted
                // Example: If controller X defines "test" header but user can't see X,
                // then the next permitted controller Y will still get "test" header applied
                if (segmentMenuHeader != null)
                {
                    currentHeader = segmentMenuHeader;
                }

                // For permitted controllers only: apply the current header inheritance
                if (segment != null)
                {
                    // If this permitted segment doesn't define its own header,
                    // inherit the current header from previous controllers (including non-permitted ones)
                    if (currentHeader != null && segment.General.MenuHeader == null)
                    {
                        segment.General.MenuHeader = currentHeader;
                    }

                    permittedSegments.Add(segment);
                }
            }

            return permittedSegments.Where(s => s != null).ToList();
        }

        private async Task<List<AdminSettingsSegment>> GetAllSettingsInAssembly()
        {
            var assembly = Assembly.GetAssembly(GetType());
            var baseSettingsType = typeof(AdminBaseSettings);
            
            var allSettingsTypes = assembly.GetTypes()
                .Where(p => baseSettingsType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);


            // Check permissions.
            var permittedSettings = new List<AdminSettingsSegment>();
            foreach (var settingType in allSettingsTypes)
            {
                var currentUserRoleIdentifier = GetCurrentAdminUserRoleIdentifier();
                var currentUserRoleType = GetCurrentAdminUserRoleType();
                var permissionAttr = settingType.GetCustomAttribute<AdminPermissionAttribute>();
                if (PermissionHelper.CheckPermission(permissionAttr, currentUserRoleIdentifier, currentUserRoleType, PermissionType.View))
                {
                    var settingDescription = settingType.GetCustomAttribute<DescriptionAttribute>();
                    if (settingDescription == null)
                    {
                        throw new Exception($"Settings type {settingType.Name} is missing the DescriptionAttribute and cannot be processed for admin display.");
                    }

                    permittedSettings.Add(new AdminSettingsSegment
                    {
                        Id = settingType.Name,
                        Name = settingDescription.Description ?? settingType.Name, 
                        ControllerName = "AdminSettings"
                    });
                }
            }
            return permittedSettings.OrderBy(s => s.Name).ToList(); 
        }
    }
}
