using Dino.Core.AdminBL.Settings;
using Dino.CoreMvc.Admin.ModelsSettings;
using DinoGenericAdmin.Api.Controllers.Base;
using DinoGenericAdmin.Api.ModelsSettings;
using DinoGenericAdmin.BL.Data;
using Microsoft.AspNetCore.Mvc;
using Dino.Core.AdminBL.Cache;
using System.Threading.Tasks;

namespace DinoGenericAdmin.Api.Controllers
{
    public class HomeController : MainAppBaseController<HomeController>
    {

        #region Dummy entry point

        public IActionResult Index()
        {
            return new ContentResult
            {
                ContentType = "text/html",
                Content = 
                      "<html><head><title>Dino Admin</title></head><body style='font-family:arial; padding-top: 5px; padding-left: 5px'>" +
                      "<h1>Made by Dino Tech Solutions</h1>" +
                      "<pre>" +
                      " _______________________________________________________________________<br>" +
                      "|                                                                       |<br>" +
                      "|                  " +
                      "Visit us at: <a href='https://devdino.com' target='_blank'>https://devdino.com</a>" +
                      "                     |<br>" +
                      "\\_______________  ______________________________________________________/<br>" +
                      "            __  )/\n" +
                      "           / _)\n" +
                      "    .-^^^-/ /\n" +
                      " __/       /\n" +
                      "<__.|_|-|_|</pre>" +
                      "</body></html>"
            };
        }

        #endregion

        //[HttpGet]
        //public JsonResult Test()
        //{
        //    var db = BLFactory.GetNewContext();
		//
        //    var name = db.Items.FirstOrDefault()?.Name;
		//
        //    var items = db.Items.Take(10).ToList();
        //    var data = String.Join(", ", items.Select(x => x.Name));
        //    return CreateJsonResponse(true, data, null, true);
        //}
        
        //[HttpGet]
        //public JsonResult GetSystemSettings()
        //{
        //    // Get the SystemSettings using the static AppSettings class
        //    // Note that we can request either the concrete class or the abstract base class
        //    
        //    // Option 1: Get the concrete implementation directly
        //    var settings = AppSettings.Get<SystemSettings>();
        //    
        //    // Option 2: Get via the abstract base class (it will return the concrete implementation)
        //    var dinoMasterSettings = AppSettings.Get<DinoMasterSettings>();
        //    
        //    
        //    return CreateJsonResponse(true, dinoMasterSettings, null, true);
        //}
        
        //[HttpGet]
        //public JsonResult GetSettingsViaInterface()
        //{
        //    // Get settings via the interface - finds the most appropriate implementation
        //    var settings = AppSettings.Get<IAdminBaseSettings>();
            
        //    // Since the result is just the interface, we need to cast to access specific properties
        //    // Here we'll check for various possible implementations
        //    var response = new Dictionary<string, object>
        //    {
        //        // Base interface properties are always available
        //        ["ClassName"] = settings.ClassName,
        //        ["Name"] = settings.Name,
        //        ["LastUpdated"] = settings.UpdateDate
        //    };
            
        //    // Check for specific implementations and add their properties
        //    if (settings is SystemSettings systemSettings)
        //    {
        //        response["SiteName"] = systemSettings.SiteName;
        //        response["SiteDescription"] = systemSettings.SiteDescription;
        //        response["DefaultLanguage"] = systemSettings.DefaultLanguage;
        //    }
            
        //    return CreateJsonResponse(true, response, null, true);
        //}
        
        //[HttpGet]
        //public JsonResult SettingsWithChangeNotification()
        //{
        //    // Register for change notifications for SystemSettings
        //    AppSettings.OnChanged<SystemSettings>(() => 
        //    {
        //        // This callback will be triggered when settings are updated
        //        Logger.LogInformation("SystemSettings were updated, we can refresh cached values if needed");
        //    });
        //    
        //    // You can also register for notifications on the interface level
        //    AppSettings.OnChanged<IAdminBaseSettings>(() =>
        //    {
        //        // This will be triggered when ANY settings type is updated
        //        Logger.LogInformation("Any settings were updated");
        //    });
        //    
        //    // Get the current settings
        //    var settings = AppSettings.Get<SystemSettings>();
        //    
        //    return CreateJsonResponse(true, "Change notification registered", null, true);
        //}
		//
        //[HttpGet]
        //public async Task<JsonResult> TestCache()
        //{
        //    Logger.LogInformation("--- Starting Cache Test ---");
        //    var results = new List<string>();
        //    int testItemId = 1; // Assuming an item with ID 1 exists
        //    int testCategoryId = 1; // Assuming a category with ID 1 exists
		//
        //    // --- Test Item (OnDemand) ---
        //    results.Add($"Attempt 1: Getting Item {testItemId} (Expect Cache MISS if first time, then load)");
        //    Logger.LogInformation(results.Last());
        //    var item1 = await CacheManager.GetOrCreate<ItemCacheModel, int>(testItemId);
        //    results.Add($"Attempt 1 Result: {(item1 != null ? $"Got Item: {item1.Name}" : "Item not found")}");
        //    Logger.LogInformation(results.Last());
		//
        //    await Task.Delay(100); // Small delay for clarity in logs if needed
		//
        //    results.Add($"Attempt 2: Getting Item {testItemId} again (Expect Cache HIT)");
        //    Logger.LogInformation(results.Last());
        //    var item2 = await CacheManager.GetOrCreate<ItemCacheModel, int>(testItemId);
        //    results.Add($"Attempt 2 Result: {(item2 != null ? $"Got Item: {item2.Name}" : "Item not found")}");
        //    Logger.LogInformation(results.Last());
		//
        //    bool itemTestSuccess = item1 != null && item2 != null && item1.Id == item2.Id;
        //    results.Add($"Item OnDemand Test {(itemTestSuccess ? "PASSED" : "FAILED")}: First load should show MISS, second should not.");
        //    Logger.LogInformation(results.Last());
		//
        //    results.Add("-------------------------------------");
        //    Logger.LogInformation("-------------------------------------");
		//
        //    // --- Test Category (OnApplicationStart) ---
        //    // Note: Verification requires checking if LoadAll was called on startup (logs) and if this Get causes a MISS.
        //    results.Add($"Attempt 1: Getting Category {testCategoryId} (Expect Cache HIT if loaded on startup)");
        //    Logger.LogInformation(results.Last());
        //    var category1 = await CacheManager.GetOrCreate<ItemCategoryCacheModel, int>(testCategoryId);
        //    results.Add($"Attempt 1 Result: {(category1 != null ? $"Got Category: {category1.Name}" : "Category not found")}");
        //    Logger.LogInformation(results.Last());
		//
        //    await Task.Delay(100);
		//
        //    results.Add($"Attempt 2: Getting Category {testCategoryId} again (Expect Cache HIT)");
        //    Logger.LogInformation(results.Last());
        //    var category2 = await CacheManager.GetOrCreate<ItemCategoryCacheModel, int>(testCategoryId);
        //    results.Add($"Attempt 2 Result: {(category2 != null ? $"Got Category: {category2.Name}" : "Category not found")}");
        //    Logger.LogInformation(results.Last());
		//
        //    bool categoryTestSuccess = category1 != null && category2 != null && category1.Id == category2.Id;
        //    results.Add($"Category OnApplicationStart Test {(categoryTestSuccess ? "PASSED (Check logs for MISS)" : "FAILED")}: Should ideally show no MISS logs from these calls.");
        //    Logger.LogInformation(results.Last());
		//
        //    Logger.LogInformation("--- Cache Test Finished ---");
		//
        //    return CreateJsonResponse(true, results, null, true);
        //}
    }
}
