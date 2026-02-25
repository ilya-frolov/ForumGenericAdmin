using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins;
using Dino.CoreMvc.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.ComponentModel.DataAnnotations;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.ComponentModel;

namespace Dino.CoreMvc.Admin.Controllers
{
    public class ImportResult
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }

    public class ImportError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
    }

    public abstract partial class DinoAdminBaseEntityController<TModel, TEFEntity, TIdType>
    {

        /// <summary>
        /// Alternative template download method to diagnose stream issues
        /// </summary>
        [HttpGet]
        public virtual IActionResult DownloadImportTemplate()
        {
            try
            {
                // Get controller name for file prefix
                var controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                var templateName = $"{controllerName}_ImportTemplate_{DateTime.Now:yyyyMMdd}.xlsx";

                // Create Excel workbook
                IWorkbook workbook = new XSSFWorkbook(); // XLSX format
                ISheet worksheet = workbook.CreateSheet("Import Template");

                // Get model properties from TModel
                var properties = typeof(TModel).GetProperties()
                    .Where(p => p.CanWrite && !p.GetCustomAttributes<SkipMappingAttribute>().Any(a => a.SkipImport))
                    .ToList();

                // Create header row with styles
                IRow headerRow = worksheet.CreateRow(0);
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                IFont headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                // Add sample data row
                IRow sampleRow = worksheet.CreateRow(1);

                // Add headers based on properties
                for (int i = 0; i < properties.Count; i++)
                {
                    var prop = properties[i];
                    if (prop.Name.ToLower() == "id")
                    {
                        continue;
                    }
                    int columnIndex = i;

                    // Try to get display name from attributes
                    var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                    var adminFieldAttr = prop.GetCustomAttribute<AdminFieldCommonAttribute>();

                    var headerName = displayAttr?.Name
                        ?? adminFieldAttr?.Name
                        ?? prop.Name;

                    // Set header value and style
                    ICell headerCell = headerRow.CreateCell(columnIndex);
                    headerCell.SetCellValue(headerName);
                    headerCell.CellStyle = headerStyle;

                    // Add sample data in row 2
                    var sampleValue = GenerateSampleValueForProperty(prop);
                    ICell sampleCell = sampleRow.CreateCell(columnIndex);
                    SetCellValue(sampleCell, sampleValue, prop.PropertyType);

                    // Auto-size column
                    worksheet.AutoSizeColumn(columnIndex);
                }

                // Add to file in temp location
                string tempFilePath = Path.Combine(Path.GetTempPath(), templateName);
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    workbook.Write(fileStream);
                }

                // Return physical file
                return PhysicalFile(tempFilePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating import template");
                return BadRequest("Error generating import template: " + ex.Message);
            }
        }

        /// <summary>
        /// Imports data from an Excel or CSV file
        /// </summary>
        [HttpPost]
        public virtual async Task<JsonResult> Import(IFormFile file, [FromForm] int batchSize = 1000, [FromForm] string refId = null)
        {
            if (!await CheckPermission(PermissionType.Import, refId))
            {
                return CreateJsonResponse(false, null, $"You do not have permission to import this entity.", false);
            }

            if (file == null || file.Length == 0)
            {
                return CreateJsonResponse(false, null, "No file was uploaded");
            }

            // Check file extension
            string extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
            {
                return CreateJsonResponse(false, null, "Only Excel and CSV files are supported");
            }

            var result = new ImportResult();

            try
            {
                // Read the entire file into memory first to avoid stream disposal issues
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Create workbook based on file extension
                    IWorkbook workbook;
                    if (extension == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(memoryStream);
                    }
                    else if (extension == ".xls")
                    {
                        workbook = new HSSFWorkbook(memoryStream);
                    }
                    else // CSV
                    {
                        // For CSV, convert to Excel format first
                        using (var reader = new StreamReader(memoryStream))
                        {
                            // Reset stream for reading
                            memoryStream.Position = 0;
                            workbook = ConvertCsvToWorkbook(reader);
                        }
                    }

                    using (workbook)
                    {
                        // Get the first worksheet
                        ISheet worksheet = workbook.GetSheetAt(0);
                        if (worksheet == null)
                        {
                            return CreateJsonResponse(false, null, "The file does not contain any worksheets");
                        }

                        // Get row and column count
                        int rowCount = worksheet.LastRowNum + 1;

                        if (rowCount <= 1)
                        {
                            return CreateJsonResponse(false, null, "The file does not contain any data rows");
                        }

                        // Read headers from first row
                        IRow headerRow = worksheet.GetRow(0);
                        if (headerRow == null)
                        {
                            return CreateJsonResponse(false, null, "Header row is missing");
                        }

                        int colCount = headerRow.LastCellNum;
                        var headers = new Dictionary<int, string>();

                        for (int col = 0; col < colCount; col++)
                        {
                            ICell cell = headerRow.GetCell(col);
                            if (cell != null)
                            {
                                string headerValue = cell.StringCellValue?.Trim();
                                if (!string.IsNullOrEmpty(headerValue))
                                {
                                    headers[col] = headerValue;
                                }
                            }
                        }

                        // Get model properties
                        var properties = typeof(TModel).GetProperties()
                            .Where(p => p.CanWrite)
                            .ToList();

                        // Get the reference column property if refId is provided
                        PropertyInfo referenceColumnProperty = null;
                        if (refId.IsNotNullOrEmpty())
                        {
                            referenceColumnProperty = GetModelPropertyWithAttribute<ParentReferenceColumnAttribute>();
                        }

                        // Map headers to properties
                        var propertyMap = new Dictionary<int, PropertyInfo>();
                        foreach (var header in headers)
                        {
                            int col = header.Key;
                            string headerValue = header.Value;

                            // Try to match by property name
                            var property = properties.FirstOrDefault(p =>
                                string.Equals(p.Name, headerValue, StringComparison.OrdinalIgnoreCase) &&
                                !p.GetCustomAttributes<SkipMappingAttribute>().Any(a => a.SkipImport));

                            // If not found, try to match by DisplayAttribute or AdminFieldCommon
                            if (property == null)
                            {
                                property = properties.FirstOrDefault(p =>
                                {
                                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                                    var adminFieldAttr = p.GetCustomAttribute<AdminFieldCommonAttribute>();
                                    var skipAttr = p.GetCustomAttribute<SkipMappingAttribute>();

                                    return (skipAttr == null || !skipAttr.SkipImport) &&
                                           ((displayAttr != null && string.Equals(displayAttr.Name, headerValue, StringComparison.OrdinalIgnoreCase)) ||
                                            (adminFieldAttr != null && string.Equals(adminFieldAttr.Name, headerValue, StringComparison.OrdinalIgnoreCase)));
                                });
                            }

                            if (property != null)
                            {
                                propertyMap[col] = property;
                            }
                        }

                        if (!propertyMap.Any())
                        {
                            return CreateJsonResponse(false, null, "Could not match any columns to model properties");
                        }

                        // Process data in batches
                        result.TotalRecords = rowCount - 1; // Exclude header row
                        var efModelsToSave = new List<TEFEntity>();
                        var modelsToSave = new List<TModel>();

                        // Track successful saves for reporting
                        int successCount = 0;

                        for (int rowIdx = 1; rowIdx < rowCount; rowIdx++)
                        {
                            IRow row = worksheet.GetRow(rowIdx);
                            if (row == null) continue;

                            try
                            {
                                // Check if row has any data
                                bool hasData = false;
                                for (int col = 0; col < colCount; col++)
                                {
                                    ICell cell = row.GetCell(col);
                                    if (cell != null && cell.CellType != CellType.Blank)
                                    {
                                        hasData = true;
                                        break;
                                    }
                                }

                                if (!hasData) continue;

                                // Create new model instance
                                var model = Activator.CreateInstance<TModel>();

                                // Map Excel data to model properties
                                foreach (var map in propertyMap)
                                {
                                    int col = map.Key;
                                    PropertyInfo property = map.Value;

                                    ICell cell = row.GetCell(col);
                                    if (cell != null && cell.CellType != CellType.Blank)
                                    {
                                        try
                                        {
                                            // Get and convert cell value
                                            var cellValue = GetCellValue(cell);
                                            if (cellValue != null)
                                            {
                                                var convertedValue = ConvertCellValue(cellValue, property.PropertyType);
                                                property.SetValue(model, convertedValue);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            result.Errors.Add(new ImportError
                                            {
                                                RowNumber = rowIdx + 1, // 1-based row number for user
                                                FieldName = property.Name,
                                                ErrorMessage = $"Invalid value format: {ex.Message}"
                                            });
                                        }
                                    }
                                }

                                // Apply refId to parent reference column if provided
                                if (refId.IsNotNullOrEmpty() && referenceColumnProperty != null)
                                {
                                    try
                                    {
                                        var convertedRefId = ConvertCellValue(refId, referenceColumnProperty.PropertyType);
                                        referenceColumnProperty.SetValue(model, convertedRefId);
                                    }
                                    catch (Exception ex)
                                    {
                                        result.Errors.Add(new ImportError
                                        {
                                            RowNumber = rowIdx + 1,
                                            FieldName = referenceColumnProperty.Name,
                                            ErrorMessage = $"Failed to set reference ID: {ex.Message}"
                                        });
                                    }
                                }

                                // Run custom mapping logic before converting to DB model
                                await CustomBeforeImportMapping(model, rowIdx + 1);

                                // Convert admin model to DB entity
                                TEFEntity dbEntity = MapToDbEntity(model);

                                // Run custom mapping logic after converting to DB model
                                await CustomAfterImportMapping(dbEntity, model, rowIdx + 1);

                                efModelsToSave.Add(dbEntity);
                                modelsToSave.Add(model);

                                // Add in batches when reaching batch size
                                if (efModelsToSave.Count >= batchSize)
                                {
                                    await CustomBeforeImportSaveAll(modelsToSave, efModelsToSave);
                                    int savedCount = await SaveImportBatch(modelsToSave, efModelsToSave, result);
                                    successCount += savedCount;
                                    await CustomAfterImportSaveAll(modelsToSave, efModelsToSave);

                                    // Clear both lists after processing the batch
                                    efModelsToSave.Clear();
                                    modelsToSave.Clear();
                                }

                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add(new ImportError
                                {
                                    RowNumber = rowIdx + 1, // 1-based row number for user
                                    FieldName = "General",
                                    ErrorMessage = ex.Message
                                });
                                result.ErrorCount++;
                            }
                        }

                        // Add remaining models
                        if (efModelsToSave.Any())
                        {
                            await CustomBeforeImportSaveAll(modelsToSave, efModelsToSave);
                            int savedCount = await SaveImportBatch(modelsToSave, efModelsToSave, result);
                            successCount += savedCount;
                            await CustomAfterImportSaveAll(modelsToSave, efModelsToSave);
                        }

                        result.SuccessCount = successCount;
                        result.ErrorCount = result.TotalRecords - successCount;

                        var isSuccess = !(result.ErrorCount > 0);


                        return CreateJsonResponse(isSuccess, result, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during import");
                return CreateJsonResponse(false, null, "Import failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Convert CSV data to an Excel workbook
        /// </summary>
        protected virtual IWorkbook ConvertCsvToWorkbook(StreamReader reader)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Sheet1");

            // Read all lines first to avoid potential stream access issues
            string[] lines = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int rowIndex = 0;

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                string[] cells = line.Split(',');
                IRow row = sheet.CreateRow(rowIndex++);

                for (int i = 0; i < cells.Length; i++)
                {
                    ICell cell = row.CreateCell(i);
                    cell.SetCellValue(cells[i].Trim());
                }
            }

            return workbook;
        }

        /// <summary>
        /// Get cell value from an Excel cell
        /// </summary>
        protected virtual object GetCellValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    // Check if it's a date
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                    return cell.NumericCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;

                case CellType.Formula:
                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.Numeric:
                            if (DateUtil.IsCellDateFormatted(cell))
                            {
                                return cell.DateCellValue;
                            }
                            return cell.NumericCellValue;

                        case CellType.String:
                            return cell.StringCellValue;

                        case CellType.Boolean:
                            return cell.BooleanCellValue;

                        default:
                            return cell.ToString();
                    }

                case CellType.Blank:
                    return null;

                default:
                    return cell.ToString();
            }
        }

        /// <summary>
        /// Set cell value based on the property type
        /// </summary>
        protected virtual void SetCellValue(ICell cell, object value, Type propertyType)
        {
            if (value == null)
            {
                cell.SetCellValue(string.Empty);
                return;
            }

            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                cell.SetCellValue(value.ToString());
            }
            else if (underlyingType == typeof(DateTime))
            {
                if (value is DateTime dateTime)
                {
                    cell.SetCellValue(dateTime);

                    // Set date format
                    ICellStyle dateStyle = cell.Sheet.Workbook.CreateCellStyle();
                    IDataFormat format = cell.Sheet.Workbook.CreateDataFormat();
                    dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
                    cell.CellStyle = dateStyle;
                }
                else
                {
                    cell.SetCellValue(value.ToString());
                }
            }
            else if (underlyingType == typeof(bool))
            {
                cell.SetCellValue((bool)value);
            }
            else if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
                     underlyingType == typeof(float) || underlyingType == typeof(double) ||
                     underlyingType == typeof(decimal))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else if (underlyingType.IsEnum)
            {
                // Use enum description, name, or numeric value for better readability
                cell.SetCellValue(GetEnumDisplayValue(underlyingType, value));
            }
            else
            {
                cell.SetCellValue(value.ToString());
            }
        }

        /// <summary>
        /// Gets the display value for an enum - tries description first, then name, then numeric value
        /// </summary>
        /// <param name="enumType">The enum type</param>
        /// <param name="value">The enum value</param>
        /// <returns>The display value for the enum</returns>
        protected virtual string GetEnumDisplayValue(Type enumType, object value)
        {
            if (value == null) return string.Empty;

            try
            {
                var enumName = Enum.GetName(enumType, value);
                if (enumName == null) return value.ToString();

                // Try to get description attribute
                var field = enumType.GetField(enumName);
                if (field != null)
                {
                    var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                    if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                    {
                        return descriptionAttribute.Description;
                    }
                }

                // Fall back to enum name
                return enumName;
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Generates a sample value for a property based on its type
        /// </summary>
        protected virtual object GenerateSampleValueForProperty(PropertyInfo property)
        {
            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType == typeof(string))
            {
                return $"Sample {property.Name}";
            }
            else if (propertyType == typeof(int) || propertyType == typeof(long))
            {
                return 1234;
            }
            else if (propertyType == typeof(decimal) || propertyType == typeof(double) || propertyType == typeof(float))
            {
                return 123.45;
            }
            else if (propertyType == typeof(DateTime))
            {
                return DateTime.Now;
            }
            else if (propertyType == typeof(bool))
            {
                return true;
            }
            else if (propertyType.IsEnum)
            {
                // Get first enum value
                var enumValues = Enum.GetValues(propertyType);
                if (enumValues.Length > 0)
                {
                    return enumValues.GetValue(0);
                }
                return 0;
            }

            return null;
        }

        /// <summary>
        /// Maps an admin model to a database entity
        /// </summary>
        protected virtual TEFEntity MapToDbEntity(TModel model)
        {
            // This method should be implemented by derived classes if ToDbModel extension is not available
            // Default implementation using reflection
            var dbEntity = Activator.CreateInstance<TEFEntity>();

            foreach (var prop in typeof(TModel).GetProperties())
            {
                var dbProp = typeof(TEFEntity).GetProperty(prop.Name);
                if (dbProp != null && dbProp.CanWrite && prop.CanRead)
                {
                    var value = prop.GetValue(model);
                    if (value != null && dbProp.PropertyType.IsAssignableFrom(prop.PropertyType))
                    {
                        dbProp.SetValue(dbEntity, value);
                    }
                }
            }

            return dbEntity;
        }

        /// <summary>
        /// Converts a cell value to the target property type
        /// </summary>
        protected virtual object ConvertCellValue(object cellValue, Type targetType)
        {
            if (cellValue == null) return null;

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // String conversion is straightforward
            if (underlyingType == typeof(string))
            {
                return cellValue.ToString();
            }

            // Handle DateTime conversions
            if (underlyingType == typeof(DateTime))
            {
                if (cellValue is DateTime dateTime)
                {
                    return dateTime;
                }

                // Try parsing string representation
                if (DateTime.TryParse(cellValue.ToString(), out DateTime result))
                {
                    return result;
                }

                throw new FormatException($"Could not convert '{cellValue}' to DateTime");
            }

            // Handle Boolean values
            if (underlyingType == typeof(bool))
            {
                if (cellValue is bool boolValue)
                {
                    return boolValue;
                }

                string strValue = cellValue.ToString().ToLower();
                if (strValue == "true" || strValue == "yes" || strValue == "1")
                {
                    return true;
                }
                if (strValue == "false" || strValue == "no" || strValue == "0")
                {
                    return false;
                }

                throw new FormatException($"Could not convert '{cellValue}' to Boolean");
            }

            // Handle Enum values - support descriptions, names, and numbers
            if (underlyingType.IsEnum)
            {
                string stringValue = cellValue.ToString().Trim();

                // Try to find enum by description first
                foreach (var field in underlyingType.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                        if (descriptionAttribute != null &&
                            string.Equals(descriptionAttribute.Description, stringValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return Enum.Parse(underlyingType, field.Name);
                        }
                    }
                }

                // Try direct enum parsing by name (case-insensitive)
                if (Enum.TryParse(underlyingType, stringValue, true, out object enumResult))
                {
                    return enumResult;
                }

                // Try numeric conversion if string parsing fails
                if (int.TryParse(stringValue, out int enumValue))
                {
                    if (Enum.IsDefined(underlyingType, enumValue))
                    {
                        return Enum.ToObject(underlyingType, enumValue);
                    }
                }

                // If all fail, provide helpful error message with available values
                var availableValues = new List<string>();
                foreach (var field in underlyingType.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                        var displayValue = descriptionAttribute?.Description ?? field.Name;
                        availableValues.Add($"{field.Name} ({displayValue})");
                    }
                }

                var valuesList = string.Join(", ", availableValues);
                throw new FormatException($"Could not convert '{cellValue}' to enum type {underlyingType.Name}. Available values: {valuesList}");
            }

            // For other types, use standard conversion
            try
            {
                return Convert.ChangeType(cellValue, underlyingType);
            }
            catch (Exception ex)
            {
                // use default value if conversion fails
            }
            return Activator.CreateInstance(underlyingType);
        }

        /// <summary>
        /// Saves a batch of imported entities to the database
        /// </summary>
        protected virtual async Task<int> SaveImportBatch(List<TModel> models, List<TEFEntity> entities, ImportResult result)
        {
            if (!entities.Any()) return 0;

            int successCount = 0;

            try
            {
                // Use a new DB context for each batch to avoid tracking too many entities
                var newContext = CreateNewDbContext();
                bool shouldDisposeContext = newContext != DbContext; // Only dispose if it's a different context
                
                try
                {
                    newContext.ChangeTracker.AutoDetectChangesEnabled = false;

                    // First, add all entities to the context and run before save logic
                    var entityIndexMap = new Dictionary<TEFEntity, int>();
                    for (int i = 0; i < entities.Count; i++)
                    {
                        try
                        {
                            var entity = entities[i];
                            var model = models[i];

                            // Get ID property for checking if this is an update or new entity
                            var idProperty = typeof(TEFEntity).GetProperty("Id");
                            bool isUpdate = false;
                            
                            // Check if entity has an ID value (indicating this might be an update)
                            if (idProperty != null && idProperty.CanRead)
                            {
                                var idValue = idProperty.GetValue(entity);
                                isUpdate = idValue != null && !idValue.Equals(Activator.CreateInstance(idProperty.PropertyType));
                            }

                            // Clear ID value for new entities that are not yet saved
                            if (!isUpdate && idProperty != null && idProperty.CanWrite)
                            {
                                idProperty.SetValue(entity, Activator.CreateInstance(idProperty.PropertyType));
                            }

                            // Run custom before save logic
                            await RunCustomBeforeSavePerInportRow(newContext, model, entity);

                            // After custom logic, check again if it became an update
                            if (idProperty != null && idProperty.CanRead)
                            {
                                var idValue = idProperty.GetValue(entity);
                                isUpdate = idValue != null && !idValue.Equals(Activator.CreateInstance(idProperty.PropertyType));
                            }

                            if (isUpdate)
                            {
                                // For updates, find the existing entity and update it
                                var existingEntity = await newContext.Set<TEFEntity>()
                                    .FindAsync(idProperty.GetValue(entity));
                                
                                if (existingEntity != null)
                                {
                                    // Update the existing entity with new values
                                    newContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                                    newContext.Entry(existingEntity).State = EntityState.Modified;
                                }
                                else
                                {
                                    // Entity not found, treat as new
                                    if (idProperty != null && idProperty.CanWrite)
                                    {
                                        idProperty.SetValue(entity, Activator.CreateInstance(idProperty.PropertyType));
                                    }
                                    newContext.Set<TEFEntity>().Add(entity);
                                }
                            }
                            else
                            {
                                // For new entities
                                newContext.Set<TEFEntity>().Add(entity);
                            }

                            entityIndexMap[entity] = i;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Error preparing import row {RowIndex}: {Error}", i + 1, ex.Message);

                            var errorMessage = ex?.InnerException?.Message;

                            result.Errors.Add(new ImportError
                            {
                                RowNumber = i + 1,
                                FieldName = "Database",
                                ErrorMessage = $"Failed to prepare row {i + 1}: {errorMessage ?? ex?.Message}"
                            });
                        }
                    }

                    // Now save all entities in a single transaction
                    try
                    {
                        int savedCount = await newContext.SaveChangesAsync();
                        successCount = savedCount;

                        // Run custom after save logic for all successfully saved entities
                        for (int i = 0; i < entities.Count; i++)
                        {
                            try
                            {
                                var entity = entities[i];
                                var model = models[i];
                                await RunCustomAfterSavePerInportRow(newContext, model, entity);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Error in after save logic for row {RowIndex}: {Error}", i + 1, ex.Message);
                                // Don't add to errors since the entity was already saved successfully
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error saving import batch: {Error}", ex.Message);

                        var errorMessage = ex?.InnerException?.Message;

                        result.Errors.Add(new ImportError
                        {
                            RowNumber = 0,
                            FieldName = "Database",
                            ErrorMessage = $"Failed to save batch: {errorMessage ?? ex?.Message}"
                        });

                                                 successCount = 0;
                     }
                }
                finally
                {
                    // Only dispose the context if it's a different instance from the main DbContext
                    if (shouldDisposeContext && newContext != null)
                    {
                        newContext.Dispose();
                    }
                }

                return successCount;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving import batch");

                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    FieldName = "Database",
                    ErrorMessage = $"Failed to save batch: {ex.Message}"
                });

                return 0;
            }
        }

        /// <summary>
        /// Creates a new DB context for batch operations
        /// </summary>
        protected virtual DbContext CreateNewDbContext()
        {
            // By default, return the current context
            // Override in derived controller to create a new context instance if needed
            return DbContext;
        }

        /// <summary>
        /// Custom hook called before mapping imported model to DB entity
        /// </summary>
        protected virtual Task CustomBeforeImportMapping(TModel model, int rowNumber)
        {
            // Override in derived controller for custom logic
            return Task.CompletedTask;
        }

        /// <summary>
        /// Custom hook called after mapping imported model to DB entity
        /// </summary>
        protected virtual Task CustomAfterImportMapping(TEFEntity dbEntity, TModel adminModel, int rowNumber)
        {
            // Override in derived controller for custom logic
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method to add custom logic before the model save
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <param name="model">The model.</param>
        /// <param name="efModel">The EF model.</param>
        protected virtual async Task RunCustomBeforeSavePerInportRow(DbContext dbContext, TModel model, TEFEntity efModel)
        {
            // Override this method to add custom logic
        }


        /// <summary>
        /// Override this method to add custom logic after the model save
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <param name="model">The model.</param>
        /// <param name="efModel">The EF model.</param>
        protected virtual async Task RunCustomAfterSavePerInportRow(DbContext dbContext, TModel model, TEFEntity efModel)
        {
            // Override this method to add custom logic
        }


        /// <summary>
        /// Custom hook called before saving the import data  to DB entity
        /// </summary>
        protected virtual Task CustomBeforeImportSaveAll(List<TModel> model, List<TEFEntity> efModel)
        {
            // Override in derived controller for custom logic
            return Task.CompletedTask;
        }

        /// <summary>
        /// Custom hook called after saving the import data  to DB entity
        /// </summary>
        protected virtual Task CustomAfterImportSaveAll(List<TModel> model, List<TEFEntity> efModel)
        {
            // Override in derived controller for custom logic
            return Task.CompletedTask;
        }
    }
}

