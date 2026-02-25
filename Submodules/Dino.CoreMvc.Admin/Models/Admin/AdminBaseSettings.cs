using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Dino.CoreMvc.Admin.Attributes;
using Newtonsoft.Json;
using System.Reflection;
using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Logic;
using Dino.Core.AdminBL.Settings;

namespace Dino.CoreMvc.Admin.Models.Admin
{
    public class AdminBaseSettings : BaseAdminModel, IAdminBaseSettings
    {
        public override bool CustomPreMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
        {
            // If it's inner mapping, skip (after reflection invocation).
            if (dbModel is ExpandoObject)
            {
                return true;
            }

            //// Create an ExpandoObject from the existing dbModel if it exists
            dynamic expando = new System.Dynamic.ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;

            // If we have existing data, deserialize it first
            if (!string.IsNullOrEmpty(dbModel.Data))
            {
                var existingData = JsonConvert.DeserializeObject<Dictionary<string, object>>(dbModel.Data);
                foreach (var kvp in existingData)
                {
                    expandoDict[kvp.Key] = kvp.Value;
                }
            }

            // Now map from expando to our dbModel using the extension method correctly
            Dictionary<string, List<DinoAdminConvertError>> errors =
                new Dictionary<string, List<DinoAdminConvertError>>();
            ModelMappingExtensions.MapPropertiesFromJsonToModel(expando, model, context, errors, null);

            model.ClassName = dbModel.ClassName;
            model.Name = dbModel.Name;
            model.CreateDate = dbModel.CreateDate;
            model.UpdateDate = dbModel.UpdateDate;
            model.UpdateBy = dbModel.UpdateBy;

            // Return false to stop further mapping since we've handled everything
            return false;
        }

        public override bool CustomPreMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
        {
            // If it's inner mapping, skip (after reflection invocation).
            if (dbModel is ExpandoObject)
            {
                return true;
            }

            // If we have existing data, deserialize it first
            var existingData = new ExpandoObject();
            if (!string.IsNullOrEmpty(dbModel.Data))
            {
                existingData = ExpandoObjectExtensions.CreateFrom(dbModel, dbModel.Data);
            }

            // Get the actual type from the ClassName
            Type modelType = AdminSettingsController.SettingsTypeByName[dbModel.ClassName];

            // Now map from expando to our dbModel using the extension method correctly
            dynamic result = ModelMappingExtensions.ToDbEntityFromTypes(model, modelType, typeof(ExpandoObject), context, existingData);

            // Serialize the remaining data and store it in the Data property
            var dict = (IDictionary<string, object>)result.Item1;

            // Remove regular properties.
            foreach (var prop in typeof(AdminBaseSettings).GetProperties())
            {
                if (dict.ContainsKey(prop.Name))
                {
                    dict.Remove(prop.Name);
                }
            }

            dict.Remove("LazyLoader");

            dbModel.Data = JsonConvert.SerializeObject(dict);

            dbModel.UpdateDate = DateTime.Now;
            dbModel.UpdateBy = (int)context.CurrentUserId;

            // Return false to stop further mapping since we've handled everything
            return false;
        }

        [Key]
        [Required]
        [StringLength(100)]
        [AdminFieldText(maxLength: 100)]
        [JsonIgnore]
        [SkipMapping]
        public string ClassName { get; set; }

        [Required]
        [StringLength(100)]
        [AdminFieldText(maxLength: 100)]
        public string Name { get; set; }

        [Required]
        [SkipMapping]
        private string Data { get; set; }

        [VisibilitySettings(showOnCreate: false)]
        [SaveDate]
        [JsonIgnore]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [AdminFieldDateTime]
        [VisibilitySettings(showOnCreate: false)]
        [LastUpdateDate]
        [JsonIgnore]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [UpdatedBy]
        [JsonIgnore]
        public int UpdateBy { get; set; } = 1;
    }
}