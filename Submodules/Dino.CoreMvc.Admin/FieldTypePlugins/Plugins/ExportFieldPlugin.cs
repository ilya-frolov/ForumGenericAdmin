using ClosedXML.Excel;
using CsvHelper;
using Dino.CoreMvc.Admin.Attributes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.ComponentModel;


namespace Dino.CoreMvc.Admin.FieldTypePlugins.Plugins
{
    /// <summary>
    /// Plugin for handling export functionality in the admin interface.
    /// Provides methods for exporting data to Excel, CSV, and PDF formats.
    /// </summary>
    public class ExportFieldPlugin : BaseFieldTypePlugin<AdminFieldBaseAttribute, object>
    {
        /// <summary>
        /// Initializes a new instance of the ExportFieldPlugin.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        public ExportFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Gets the field type identifier for the export plugin.
        /// </summary>
        public override string FieldType => "Export";

        /// <summary>
        /// Validates the export field value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="property">The property information.</param>
        /// <returns>A tuple containing validation result and error messages.</returns>
        public override (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property)
        {
            return (true, new List<string>());
        }

        /// <summary>
        /// Prepares the value for database storage.
        /// </summary>
        /// <param name="value">The value to prepare.</param>
        /// <param name="property">The property information.</param>
        /// <returns>The prepared value.</returns>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            return value;
        }

        /// <summary>
        /// Gets a list of properties that can be exported for a given type.
        /// </summary>
        /// <typeparam name="T">The type to get exportable properties for.</typeparam>
        /// <returns>A list of PropertyInfo objects that can be exported.</returns>
        private static List<PropertyInfo> GetExportableProperties<T>()
        {
            return typeof(T).GetProperties()
                 .Where(p => p.GetCustomAttributes()
                     .Any(attr => attr is AdminFieldBaseAttribute) &&
                     !p.GetCustomAttributes()
                     .Any(attr => attr is AdminSkipExportAttribute))
                 .ToList();
        }

        /// <summary>
        /// Exports data to an Excel file.
        /// </summary>
        /// <typeparam name="T">The type of data to export.</typeparam>
        /// <param name="data">The data to export.</param>
        /// <param name="filePath">The path where the Excel file should be saved.</param>
        /// <param name="sheetName">The name of the worksheet (default: "Sheet1").</param>
        public static void ExportToExcel<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            var properties = GetExportableProperties<T>();

            // Write headers
            for (int i = 0; i < properties.Count; i++)
            {
                var commonAttribute = properties[i].GetCustomAttribute<AdminFieldCommonAttribute>();
                worksheet.Cell(1, i + 1).Value = commonAttribute?.Name ?? properties[i].Name;
            }

            // Write data
            int row = 2;
            foreach (var item in data)
            {
                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].GetValue(item);
                    worksheet.Cell(row, col + 1).Value = FormatPropertyValue(value, properties[col]);
                }
                row++;
            }

            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Exports data to a CSV file.
        /// </summary>
        /// <typeparam name="T">The type of data to export.</typeparam>
        /// <param name="data">The data to export.</param>
        /// <param name="filePath">The path where the CSV file should be saved.</param>
        public static void ExportToCsv<T>(IEnumerable<T> data, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var properties = GetExportableProperties<T>();

            // Write headers
            foreach (var property in properties)
            {
                var commonAttribute = property.GetCustomAttribute<AdminFieldCommonAttribute>();
                csv.WriteField(commonAttribute?.Name ?? property.Name);
            }
            csv.NextRecord();

            // Write data
            foreach (var item in data)
            {
                foreach (var property in properties)
                {
                    var value = property.GetValue(item);
                    csv.WriteField(FormatPropertyValue(value, property));
                }
                csv.NextRecord();
            }
        }

        /// <summary>
        /// Exports data to a PDF file.
        /// </summary>
        /// <typeparam name="T">The type of data to export.</typeparam>
        /// <param name="data">The data to export.</param>
        /// <param name="filePath">The path where the PDF file should be saved.</param>
        public static void ExportToPdf<T>(IEnumerable<T> data, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var properties = GetExportableProperties<T>();

            // Get headers
            var headers = properties.Select(p =>
            {
                var commonAttribute = p.GetCustomAttribute<AdminFieldCommonAttribute>();
                return commonAttribute?.Name ?? p.Name;
            }).ToList();

            var columnWidths = new List<float>();
            foreach (var header in headers)
            {
                // Start with header length
                int maxLength = header.Length;

                // Check data length in each column
                foreach (var item in data)
                {
                    var prop = properties[headers.IndexOf(header)];
                    var value = FormatPropertyValue(prop.GetValue(item), prop);
                    maxLength = Math.Max(maxLength, value?.Length ?? 0);
                }

                // Convert to relative width (minimum 1, maximum 4)
                columnWidths.Add(Math.Min(4, Math.Max(1, maxLength / 10f)));
            }

            // Convert data to array of strings for easier handling
            var rows = data.Select(item =>
                properties.Select(p => p.GetValue(item)?.ToString() ?? "").ToArray()
            ).ToList();

            // Generate PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {

                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);

                    page.DefaultTextStyle(x => x
                        .FontSize(9)
                        .FontColor(Colors.Black)
                        .FontFamily("Arial")
                        .Fallback(y => y.FontFamily("Segoe UI"))  // Fallback font if needed
                    );

                    page.Header().Text("Export Data").SemiBold().FontSize(14).FontFamily("Arial");

                    page.Content().Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var width in columnWidths)
                            {
                                columns.RelativeColumn(width);
                            }
                        });

                        // Add header row
                        table.Header(header =>
                        {
                            foreach (var headerText in headers)
                            {
                                header.Cell().Background("#F0F0F0").Padding(3)
                                    .Text(headerText).SemiBold();
                            }
                        });

                        // Data
                        foreach (var item in data)
                        {
                            foreach (var prop in properties)
                            {
                                var value = prop.GetValue(item);
                                table.Cell().Padding(3)
                                    .Text(FormatPropertyValue(value, prop));
                            }
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);
        }

        /// <summary>
        /// Gets the display value for an enum - tries description first, then name, then numeric value
        /// </summary>
        /// <param name="enumType">The enum type</param>
        /// <param name="value">The enum value</param>
        /// <returns>The display value for the enum</returns>
        private static string GetEnumDisplayValue(Type enumType, object value)
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
        /// Formats a property value for export, handling special cases like collections, dates, times, and enums.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="property">The property information.</param>
        /// <returns>A formatted string representation of the value.</returns>
        private static string FormatPropertyValue(object value, PropertyInfo property)
        {
            if (value == null) return string.Empty;

            // Handle List<T> types
            if (value is IEnumerable enumerable && property.PropertyType.IsGenericType)
            {
                // Skip if it's a string (which is also IEnumerable)
                if (property.PropertyType == typeof(string))
                    return value.ToString();

                // For List<SubItemModel> or complex types, return count
                if (property.PropertyType.GetGenericArguments()[0].IsClass &&
                    property.PropertyType.GetGenericArguments()[0] != typeof(string))
                {
                    return $"{enumerable.Cast<object>().Count()} items";
                }

                // For List<primitive types>, join with commas
                return string.Join(", ", enumerable.Cast<object>());
            }

            // Handle DateTime
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            // Handle TimeSpan
            if (value is TimeSpan timeSpan)
                return timeSpan.ToString(@"hh\:mm\:ss");

            // Handle Enum types - display description, name, or numeric value
            if (property.PropertyType.IsEnum || (Nullable.GetUnderlyingType(property.PropertyType)?.IsEnum == true))
            {
                var enumType = property.PropertyType.IsEnum ? property.PropertyType : Nullable.GetUnderlyingType(property.PropertyType);
                return GetEnumDisplayValue(enumType, value);
            }

            return value.ToString();
        }
    }
}