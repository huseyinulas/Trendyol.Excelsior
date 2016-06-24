﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Trendyol.Excelsior.Web
{
    public static class ExcelsiorExtensions
    {
        public static IEnumerable<T> Listify<T>(this IExcelsior excelsior, HttpPostedFileBase httpPostedFileBase, bool hasHeaderRow = false)
        {
            string fileExtension = Path.GetExtension(httpPostedFileBase.FileName) ?? String.Empty;

            IWorkbook workbook;

            switch (fileExtension.ToLower())
            {
                case ".xls":
                    workbook = new HSSFWorkbook(httpPostedFileBase.InputStream);
                    break;
                case ".xlsx":
                    workbook = new XSSFWorkbook(httpPostedFileBase.InputStream);
                    break;
                default:
                    throw new InvalidOperationException("Excelsior can only operate on .xsl and .xlsx files.");
            }

            return excelsior.Listify<T>(workbook, hasHeaderRow);
        }
    }
}