using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace ImageRecognitionApp.unit
{
    public static class ExcelToJsonExporter
    {
        public static void ExportExcelToJson(string excelFilePath)
        {
            if (!File.Exists(excelFilePath))
            {
                throw new FileNotFoundException("Excel file not found", excelFilePath);
            }

            var fileName = Path.GetFileNameWithoutExtension(excelFilePath);
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, $"{fileName}.json");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var data = new List<Dictionary<string, object>>();

                // Read headers
                var headers = new List<string>();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    headers.Add(worksheet.Cells[1, col].Text);
                }

                // Read data rows
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var rowData = new Dictionary<string, object>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        rowData[headers[col - 1]] = worksheet.Cells[row, col].Value;
                    }
                    data.Add(rowData);
                }

                // Write JSON
                File.WriteAllText(outputPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }

        public static void ExportAllExcelFiles()
        {
            var localizationDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
            if (Directory.Exists(localizationDir))
            {
                var excelFiles = Directory.GetFiles(localizationDir, "*.xlsx");
                foreach (var file in excelFiles)
                {
                    ExportExcelToJson(file);
                }
            }
        }
    }
}