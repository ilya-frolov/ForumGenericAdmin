using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dino.Common.Helpers;

namespace Dino.Common.Files
{
    public class CsvGenerator
    {
        public List<Dictionary<string, string>> Objects;

        public CsvGenerator(List<Dictionary<string, string>> objects)
        {
            Objects = objects;
        }

        public string Export()
        {
            return Export(true);
        }

        public string Export(bool includeHeaderLine)
        {
            if (Objects.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                if (includeHeaderLine)
                {
                    //add header line.
                    foreach (string keys in Objects.First().Keys)
                    {
                        sb.Append(keys).Append(",");
                    }
                    sb.Remove(sb.Length - 1, 1).Append("\r\n");
                }

                //add value for each property.
                foreach (var obj in Objects)
                {
                    foreach (var curr in obj)
                    {
                        sb.Append(MakeValueCsvFriendly(curr.Value)).Append(",");
                    }
                    sb.Remove(sb.Length - 1, 1).Append("\r\n");
                }

                return sb.ToString();
            }
            else
            {
                return null;
            }
        }

        //export to a file.
        public void ExportToFile(string path)
        {
            File.WriteAllText(path, Export());
        }

        //export as binary data.
        public byte[] ExportToBytes(bool allowEmptyFile = false)
        {
            var exportString = Export();
            if (exportString.IsNullOrEmpty())
            {
                if (!allowEmptyFile)
                {
                    throw new Exception("Export data can't be empty.");
                }
                else
                {
                    exportString = String.Empty;
                }
            }

            var data = Encoding.UTF8.GetBytes(exportString);
            return Encoding.UTF8.GetPreamble().Concat(data).ToArray();
        }

        //get the csv value for field.
        private string MakeValueCsvFriendly(object value)
        {
            if (value == null) return "";
            if (value is Nullable && ((INullable)value).IsNull) return "";

            if (value is DateTime)
            {
                if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
                    return ((DateTime)value).ToString("yyyy-MM-dd");
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }
            string output = value.ToString();

            if (output.Contains(",") || output.Contains("\""))
                output = '"' + output.Replace("\"", "\"\"") + '"';

            return output;

        }
    }
}
