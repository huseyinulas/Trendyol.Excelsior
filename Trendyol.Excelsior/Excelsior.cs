﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Trendyol.Excelsior
{
    public class Excelsior : IExcelsior
    {
        public IEnumerable<T> Listify<T>(string filePath, bool hasHeaderRow = false)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            string fileExtension = Path.GetExtension(filePath);

            FileStream stream = new FileStream(filePath, FileMode.Open);

            try
            {
                IWorkbook workbook;

                switch (fileExtension.ToLower())
                {
                    case ".xls":
                        workbook = new HSSFWorkbook(stream);
                        break;
                    case ".xlsx":
                        workbook = new XSSFWorkbook(stream);
                        break;
                    default:
                        throw new InvalidOperationException("Excelsior can only operate on .xsl and .xlsx files.");
                }

                return Listify<T>(workbook, hasHeaderRow);
            }
            finally
            {
                stream.Dispose();
            }
        }

        public IEnumerable<T> Listify<T>(IWorkbook workbook, bool hasHeaderRow = false)
        {
            if (workbook == null)
            {
                throw new ArgumentNullException("workbook");
            }

            ISheet sheet = workbook.GetSheetAt(0);

            if (sheet == null)
            {
                return Enumerable.Empty<T>();
            }

            int excelColumnCount = sheet.GetRow(0).PhysicalNumberOfCells;

            List<PropertyInfo> mappingTypeProperties = GetMappingTypeProperties(typeof(T));

            if (mappingTypeProperties.Count != excelColumnCount)
            {
                throw new ExcelsiorException("Column count in given file does not match columns identified in model T.");
            }

            int firstDataRow = 0;

            if (hasHeaderRow)
            {
                bool isRightColumnOrder = CheckExcelColumnOrder<T>(sheet.GetRow(0));

                if (!isRightColumnOrder)
                {
                    throw new ExcelsiorException("Columns order identified in model T does not match the order in file.");
                }

                firstDataRow = 1;
            }

            ICollection<T> itemList = new List<T>();

            for (int i = firstDataRow; i < sheet.PhysicalNumberOfRows; i++)
            {
                IRow row = sheet.GetRow(i);

                bool isRowEmpty = IsRowEmpty(row);

                if (isRowEmpty)
                {
                    continue;
                }

                T item = Activator.CreateInstance<T>();

                PropertyInfo prop = typeof(T).GetProperty("RowNo");

                if (prop != null)
                {
                    prop.SetValue(item, i);
                }

                foreach (PropertyInfo pi in mappingTypeProperties)
                {
                    ExcelColumnAttribute attr = pi.GetCustomAttribute<ExcelColumnAttribute>();

                    if (attr != null)
                    {
                        int columnOrder = attr.Order - 1;

                        string columnValue = row.GetCell(columnOrder, MissingCellPolicy.RETURN_BLANK_AS_NULL) == null ? string.Empty : row.GetCell(columnOrder, MissingCellPolicy.RETURN_BLANK_AS_NULL).ToString();

                        if (!string.IsNullOrEmpty(columnValue))
                        {
                            columnValue = columnValue.Trim();

                            if (pi.PropertyType == typeof(int))
                            {
                                int val;

                                if (int.TryParse(columnValue, out val))
                                {
                                    pi.SetValue(item, val);
                                }
                            }
                            else if (pi.PropertyType == typeof(decimal))
                            {
                                decimal val;

                                if (decimal.TryParse(columnValue, out val))
                                {
                                    pi.SetValue(item, Math.Round(val, 2));
                                }
                            }
                            else if (pi.PropertyType == typeof(float))
                            {
                                float val;

                                if (float.TryParse(columnValue, out val))
                                {
                                    pi.SetValue(item, Math.Round(val, 2));
                                }
                            }
                            else if (pi.PropertyType == typeof(long))
                            {
                                long val;

                                if (long.TryParse(columnValue, out val))
                                {
                                    pi.SetValue(item, val);
                                }
                            }
                            else if (pi.PropertyType == typeof(DateTime))
                            {
                                DateTime val;

                                if (DateTime.TryParse(columnValue, out val))
                                {
                                    pi.SetValue(item, val);
                                }
                            }
                            else if (pi.PropertyType == typeof(string))
                            {
                                pi.SetValue(item, columnValue);
                            }
                        }
                    }
                }

                itemList.Add(item);
            }

            return itemList;
        }

        private List<PropertyInfo> GetMappingTypeProperties(Type type)
        {
            return type.GetProperties().Where(p => p.GetCustomAttribute<ExcelColumnAttribute>() != null).ToList();
        }

        private bool CheckExcelColumnOrder<T>(IRow headerRow)
        {
            List<PropertyInfo> properties = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ExcelColumnAttribute>() != null).ToList();

            if (headerRow != null)
            {
                for (int i = 0; i < headerRow.Cells.Count; i++)
                {
                    string celValue = headerRow.Cells[i].StringCellValue;

                    PropertyInfo pi = properties.FirstOrDefault(p => p.GetCustomAttribute<ExcelColumnAttribute>() != null && p.GetCustomAttribute<ExcelColumnAttribute>().Name == celValue);

                    if (pi == null)
                    {
                        return false;
                    }

                    if (pi.GetCustomAttribute<ExcelColumnAttribute>().Order != i + 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsRowEmpty(IRow row)
        {
            if (row == null)
            {
                return true;
            }

            for (int i = 0; i <= row.PhysicalNumberOfCells; i++)
            {
                ICell cell = row.GetCell(i);

                if (cell != null && cell.CellType != CellType.Blank)
                {
                    return false;
                }
            }

            return true;
        }
    }
}