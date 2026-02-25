using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Dino.Common.Helpers;
using DocumentFormat.OpenXml;

namespace Dino.Infra.Excel
{
    public static class ExcelWriter
    {
        /// <summary>
        /// Exports a list (or enumerable) to excel spreadsheet.
        /// Use a list of ExpandoObject (as IDictionary<string, Object>) in order to make dynamic names for the columns, even in Hebrew.
        /// Can use other types as well, but works best with ExpandoObject.
        /// </summary>
        /// <typeparam name="T">The list's type.</typeparam>
        /// <param name="objects">The list the export.</param>
        /// <param name="headerLabels">OPTIONAL: A list of labels to write as the spreadsheet's header. MAY CAUSE ERRORS FOR NOW WITH CELL FORMATTING!!</param>
        /// Relevant only if exportToResponseStream is set to TRUE.</param>
        /// <param name="rtlWorksheet">Do we need to excel to be RTL (right-to-left) instead of LTR by default.</param>
        /// <param name="worksheetName">The worksheet's name.</param>
        /// <returns>The excel package.</returns>
        public static XLWorkbook GenerateExcelPackageWithHeaders<T>(this IEnumerable<T> objects,
            IEnumerable<string> headerLabels = null, bool rtlWorksheet = false, bool autoFormatColumns = true, string worksheetName = "Worksheet")
            where T : class
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(worksheetName);
            worksheet.RightToLeft = rtlWorksheet;

            // If the collection is not ExpandoObject, convert it.
            IEnumerable<ExpandoObject> objectList;
            if (!typeof(ExpandoObject).IsAssignableFrom(typeof(T)))
            {
                var expandoCollection = new List<ExpandoObject>();
                var properties = typeof(T).GetProperties();
                foreach (var element in objects)
                {
                    IDictionary<string, object> expando = new ExpandoObject();

                    foreach (var propertyInfo in properties)
                    {
                        var currentValue = propertyInfo.GetValue(element);
                        expando.Add(propertyInfo.Name, currentValue);
                    }

                    expandoCollection.Add((ExpandoObject)expando);
                }

                objectList = expandoCollection;
            }
            else
            {
                objectList = (IEnumerable<ExpandoObject>)objects;
            }

            var collection = objectList.ToList();
            if (collection.Count > 0)
            {
                var properties = collection.First().ToDictionary(x => x.Key, x => x.Value);

                // Try to fill empty properties.
                for (int i = 0; i < properties.Count; i++)
                {
                    var property = properties.ElementAt(i);
                    if (property.Value == null)
                    {
                        // Try to get from other element in the collection.
                        object obj = collection.FirstOrDefault(x =>
                            ((IDictionary<string, object>)x)[property.Key] != null);
                        object value = ((IDictionary<string, object>)obj)?[property.Key];

                        properties[property.Key] = value;
                    }
                }

                var headers = headerLabels ?? properties.Keys;
                int colIndex = 1;
                foreach (var header in headers)
                {
                    worksheet.Cell(1, colIndex).Value = header;
                    worksheet.Cell(1, colIndex).Style.Font.Bold = true;
                    worksheet.Cell(1, colIndex).Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); //Set color to dark blue
                    worksheet.Cell(1, colIndex).Style.Fill.PatternType = XLFillPatternValues.Solid;
                    worksheet.Cell(1, colIndex).Style.Font.FontColor = XLColor.White;
                    colIndex++;
                }

                worksheet.SheetView.FreezeRows(1); // Freeze header row

                int rowIndex = 2;
                foreach (var currItem in collection)
                {
                    var currItemData = (IDictionary<string, object>)currItem;

                    colIndex = 1;
                    foreach (var currProperty in properties)
                    {
                        var currCell = worksheet.Cell(rowIndex, colIndex);

                        currItemData.TryGetValue(currProperty.Key, out var currValue);

                        if (currProperty.Value == null)
                        {
                            if (currValue != null)
                            {
                                currCell.Value = currValue.ToString();
                            }
                        }
                        else if (((currProperty.Value is int) ||
                             (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(int))) ||
                            ((currProperty.Value is long) ||
                             (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(long))))
                        {
                            if (currValue != null)
                            {
                                currCell.Value = Convert.ToInt64(currValue);
                            }
                            
                            currCell.Style.NumberFormat.Format = "0";
                        }
                        else if (((currProperty.Value is double) ||
                                  (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(double))) ||
                                 ((currProperty.Value is float) ||
                                  (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(float))))
                        {
                            if (currValue != null)
                            {
                                currCell.Value = Convert.ToDouble(currValue);
                            }
                            
                            currCell.Style.NumberFormat.Format = "0.00";
                        }
                        else if ((currProperty.Value is DateTime) || (currProperty.Value is DateTime?) ||
                                 (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(DateTime)))
                        {
                            if (currValue != null)
                            {
                                if (currValue is DateTime dateTimeValue)
                                {
                                    currCell.Value = dateTimeValue;
                                }
                                else if (DateTime.TryParse(currValue.ToString(), out var parsedDateTime))
                                {
                                    currCell.Value = parsedDateTime;
                                }
                                else
                                {
                                    // If parsing fails, treat as string
                                    currCell.Value = currValue.ToString();
                                }
                            }
                            
                            currCell.Style.NumberFormat.Format = "dd-mm-yyyy h:mm";
                        }
                        else if ((currProperty.Value is TimeSpan) || (currProperty.Value is TimeSpan?) ||
                                 (Nullable.GetUnderlyingType(currProperty.Value.GetType()) == typeof(TimeSpan)))
                        {
                            if (currValue != null)
                            {
                                if (currValue is TimeSpan timeSpanValue)
                                {
                                    currCell.Value = timeSpanValue;
                                }
                                else if (TimeSpan.TryParse(currValue.ToString(), out var parsedTimeSpan))
                                {
                                    currCell.Value = parsedTimeSpan;
                                }
                                else
                                {
                                    // If parsing fails, treat as string
                                    currCell.Value = currValue.ToString();
                                }
                            }
                            
                            currCell.Style.NumberFormat.Format = "h:mm";
                        }
                        else if (currProperty.Value is string && Uri.IsWellFormedUriString(currProperty.Value.ToString(), UriKind.Absolute))
                        {
                            currCell.Style.Font.Underline = XLFontUnderlineValues.Single;
                            currCell.Style.Font.FontColor = XLColor.Blue;

                            if (currValue != null)
                            {
                                currCell.SetHyperlink(new XLHyperlink(new Uri(currValue.ToString())));
                                currCell.Value = currValue.ToString();
                            }
                        }
                        else if (currValue != null)
                        {
                            currCell.Value = currValue.ToString();
                        }

                        colIndex++;
                    }

                    rowIndex++;
                }

                // Auto format.
                if (autoFormatColumns)
                {
                    worksheet.Columns().AdjustToContents();
                }

            }

            return workbook;
        }

        /// <summary>
        /// Exports a list (or enumerable) to excel spreadsheet.
        /// Use a list of ExpandoObject (as IDictionary<string, Object>) in order to make dynamic names for the columns, even in Hebrew.
        /// Can use other types as well, but works best with ExpandoObject.
        /// </summary>
        /// <typeparam name="T">The list's type.</typeparam>
        /// <param name="objects">The list the export.</param>
        /// <param name="headerLabels">OPTIONAL: A list of labels to write as the spreadsheet's header. MAY CAUSE ERRORS FOR NOW WITH CELL FORMATTING!!</param>
        /// Relevant only if exportToResponseStream is set to TRUE.</param>
        /// <param name="rtlWorksheet">Do we need to excel to be RTL (right-to-left) instead of LTR by default.</param>
        /// <param name="worksheetName">The worksheet's name.</param>
        /// <returns>A byte-array of the generated excel file.</returns>
        public static byte[] GenerateExcelWithHeaders<T>(this IEnumerable<T> objects, IEnumerable<string> headerLabels = null, bool rtlWorksheet = false, bool autoFormatColumns = true,
            string worksheetName = "Worksheet") where T : class
        {
            using (var stream = new MemoryStream())
            {
                using (var workbook = GenerateExcelPackageWithHeaders(objects, headerLabels, rtlWorksheet, autoFormatColumns, worksheetName))
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public static byte[] ConvertWorkbookObjectToBytes(object workbookObject)
        {
            using (var stream = new MemoryStream())
            {
                using (var workbook = (XLWorkbook)workbookObject)
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Joins 2 excel workbooks into a single workbook (add the worksheets together).
        /// </summary>
        /// <param name="workbooks">The list of workbooks that include the worksheets.</param>
        /// <returns>The joined workbooks.</returns>
        public static XLWorkbook JoinWorksheetsIntoSinglePackage(List<XLWorkbook> workbooks)
        {
            // Validate we have enough workbooks.
            if (workbooks.Count == 0)
            {
                return null;
            }
            else
            {
                var masterWorkbook = workbooks.First();

                // Add all to the first one.
                for (int i = 1; i < workbooks.Count; i++)
                {
                    var currWorkbook = workbooks[i];

                    // Add.
                    foreach (var currSheet in currWorkbook.Worksheets)
                    {
                        currSheet.CopyTo(masterWorkbook, currSheet.Name);
                    }
                }

                return masterWorkbook;
            }
        }

        /// <summary>
        /// Joins 2 excel workbooks into a single package (add the worksheets together), and return a byte array ready for download.
        /// </summary>
        /// <param name="workbooks">The list of workbooks that include the worksheets.</param>
        /// <returns>The joined workbooks as byte array, ready for download.</returns>
        public static byte[] JoinWorksheetsAsObjectIntoSingle(List<object> workbooks)
        {
            return JoinWorksheetsAsObjectIntoSingle(workbooks.SelectList(x => (XLWorkbook)x));
        }

        /// <summary>
        /// Joins 2 excel workbooks into a single package (add the worksheets together), and return a byte array ready for download.
        /// </summary>
        /// <param name="workbooks">The list of workbooks that include the worksheets.</param>
        /// <returns>The joined workbooks as byte array, ready for download.</returns>
        public static byte[] JoinWorksheetsAsObjectIntoSingle(List<XLWorkbook> workbooks)
        {
            var stream = new MemoryStream();
            using (var workbook = JoinWorksheetsIntoSinglePackage(workbooks))
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }


        public static void ConvertToCsv(string excelFilePath, string targetFile)
        {
            using (var file = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                var workbook = new XLWorkbook(file);
                ConvertToCsv(workbook, targetFile);
            }
        }

        public static void ConvertToCsv(this XLWorkbook workbook, string targetFile)
        {
            var worksheet = workbook.Worksheet(1);
            var maxColumnNumber = worksheet.LastColumnUsed().ColumnNumber();
            var totalRowCount = worksheet.LastRowUsed().RowNumber();

            using (var writer = new StreamWriter(targetFile, false, Encoding.UTF8))
            {
                for (int row = 1; row <= totalRowCount; row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= maxColumnNumber; col++)
                    {
                        rowData.Add(worksheet.Cell(row, col).GetValue<string>());
                    }
                    writer.WriteLine(string.Join(",", rowData.Select(v => $"\"{v.Replace("\"", "\"\"")}")));
                }
            }
        }

    }
}
