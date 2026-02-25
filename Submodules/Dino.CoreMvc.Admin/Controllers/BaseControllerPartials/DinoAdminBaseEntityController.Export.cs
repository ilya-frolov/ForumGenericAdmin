using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins;
using Dino.CoreMvc.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Reflection;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.CoreMvc.Admin.Attributes.Permissions;

namespace Dino.CoreMvc.Admin.Controllers
{
    public abstract partial class DinoAdminBaseEntityController<TModel, TEFEntity, TIdType>
    {
        [HttpPost]
        public virtual async Task<IActionResult> Export([FromQuery] ExportFormat format, [FromBody] ListRetrieveParams listParams,
            [FromQuery] bool showArchive, [FromQuery] bool showDeleted = false, [FromQuery] string refId = null)
        {
            try
            {
                if (!await CheckPermission(PermissionType.Export, refId))
                {
                    return CreateJsonResponse(false, null, "You do not have permission to export.", false);
                }

                var listData = await CreateListData(refId, showArchive, showDeleted, listParams);

                // Get the filename.
                var listDef = await CreateListDef(refId);
                string fileName = listDef.ExportFilename;

                // Get controller name for file prefix
                if (fileName.IsNullOrEmpty())
                {
                    fileName = this.ControllerContext.RouteData.Values["controller"].ToString();
                }

                return ExportData(listData.Items, fileName, format);
            } catch (Exception ex)
            {
                Logger.LogError(ex, "Error during export");
                return BadRequest("Export failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Exports the data in the specified format.
        /// </summary>
        /// <param name="models">The list of models to export.</param>
        /// <param name="filename">The desired name for the file.</param>
        /// <param name="format">The export format.</param>
        /// <returns>A file result containing the exported data.</returns>
        protected virtual IActionResult ExportData(List<TModel> models, string filename, ExportFormat format)
        {
            if (models == null || !models.Any())
            {
                return BadRequest("No data to export");
            }

            var fileName = $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var contentType = format switch
            {
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.Csv => "text/csv",
                ExportFormat.Pdf => "application/pdf",
                _ => throw new NotSupportedException($"Export format {format} is not supported.")
            };

            try
            {
                var ms = new MemoryStream();
                var extension = format == ExportFormat.Excel ? "xlsx" : format.ToString().ToLower();
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.{extension.ToString().ToLower()}");
                try
                {

                    switch (format)
                    {
                        case ExportFormat.Excel:
                            ExportFieldPlugin.ExportToExcel(models, tempFilePath);
                            break;
                        case ExportFormat.Csv:
                            ExportFieldPlugin.ExportToCsv(models, tempFilePath);
                            break;
                        case ExportFormat.Pdf:
                            ExportFieldPlugin.ExportToPdf(models, tempFilePath);
                            break;
                    }
                    using (var fileStream = System.IO.File.OpenRead(tempFilePath))
                    {
                        fileStream.CopyTo(ms);
                    }
                } finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempFilePath);
                        } catch (Exception ex)
                        {
                            Logger.LogError(ex, $"Error deleting temporary {tempFilePath} file");
                        }
                    }
                }

                ms.Position = 0;
                return File(ms, contentType, $"{fileName}.{format.ToString().ToLower()}");
            } catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting data");
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        protected virtual string GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}