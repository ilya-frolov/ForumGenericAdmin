using System.Reflection;
using System.Text.Json;
using Dino.Core.AdminBL.Models;
using Dino.Core.AdminBL.Settings;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using Microsoft.Extensions.Logging;

namespace Dino.CoreMvc.Admin.Controllers
{
    public class AdminSettingsController : DinoAdminBaseEntityController<AdminBaseSettings, Setting, string>
    {
        private readonly ISettingsProvider _settingsProvider;

        public override bool ErrorsOnNoneExistingEntityPropertiesMapping => false;

        private static Dictionary<string, Type> _settingsTypesByName = new();

        internal static Dictionary<string, Type> SettingsTypeByName => _settingsTypesByName;

        public AdminSettingsController(ISettingsProvider settingsProvider) : base("settings")
        {
            _settingsProvider = settingsProvider;

            if (_settingsTypesByName.Count == 0)
            {
                // Find all settings types in all loaded assemblies that might contain AdminBaseSettings subclasses
                var settingsTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException) {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(AdminBaseSettings).IsAssignableFrom(t) && t != typeof(AdminBaseSettings));


                // Register all settings types
                foreach (var type in settingsTypes)
                {
                    var instance = Activator.CreateInstance(type) as AdminBaseSettings;
                    if (instance != null)
                    {
                        _settingsTypesByName[type.Name] = type;
                    }
                }
            }   
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            return new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "Settings",
                    Priority = 0
                },
                UI = new AdminSegmentUI
                {
                    Icon = "cog",
                    IconType = IconType.PrimeIcons,
                    ShowInMenu = true,
                },
                Navigation = new AdminSegmentNavigation
                {
                    CustomPath = null,
                },
            };
        }

        protected override async Task<ListDef> CreateListDef(string refId = null)
        {
            return new ListDef
            {
                Title = "Settings",
                AllowReOrdering = false,
                AllowAdd = true,
                AllowEdit = true,
                AllowDelete = false,
                ShowArchive = false,
                ShowDeleteConfirmation = false,
            };
        }

        protected override async Task<Setting?> GetEntityById(string id)
        {
            // Try to get the entity. If you can't, it means the settings weren't created yet, so we will do this manually.
            var entity = await base.GetEntityById(id);
            if (entity == null)
            {
                var settingsType = _settingsTypesByName[id];
                if (settingsType != null)
                {
                    // Get the name of the settings type from the description attribute of the settings' class.
                    var descriptionAttribute = settingsType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                    var name = descriptionAttribute?.Description ?? id;

                    entity = new Setting
                    {
                        ClassName = id,
                        Data = "{}",
                        Name = name,
                    };

                    DbContext.Settings.Add(entity);

                    await DbContext.SaveChangesAsync();
                }
            }

            return entity;
        }

        public override Type GetAdminModelType(string id, AdminBaseSettings model, Setting entity)
        {
            // Try to get by name first
            return _settingsTypesByName[id];        // The ID of settings is the classname.
        }

        protected override async Task RunCustomAfterSave(string id, AdminBaseSettings model, Setting efModel)
        {
            // After saving the settings, reload them in the AppSettings
            try
            {
                // Trigger a reload of all settings
                await _settingsProvider.ReloadAllAsync();
                Logger.LogInformation($"Reloaded settings for {id} after save");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error reloading settings for {id}");
            }
        }
    }
} 