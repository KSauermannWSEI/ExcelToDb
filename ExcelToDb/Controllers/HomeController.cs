using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using Excel = Microsoft.Office.Interop.Excel;

namespace ExcelToDb.Controllers
{
    public class HomeController : Controller
    {
        string path = ""; //Microsoft.Office.Interop.Excel
        Excel.Application xlApp; //Microsoft.Office.Interop.Excel
        Excel.Workbook xlWorkbook; //Microsoft.Office.Interop.Excel
        Excel._Worksheet xlWorksheet; //Microsoft.Office.Interop.Excel
        Excel.Range xlRange; //Microsoft.Office.Interop.Excel
        public ActionResult Index()
        {
            return View();
        }
        private void AddToDb(List<ExcelModel> list)
        {
            var context = new ExcelEntities();
            try
            {
                context.ExcelModels.AddRange(list);
                context.SaveChanges();                
                TempData["Message"] = "Dodane";
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
            }
        }

        [HttpPost] // Nie wymaga instalacji Excela na serwrerze
        public ActionResult UploadExcelDataReader(HttpPostedFileBase File)
        {
            var list = getListExcelDataReader(File);
            AddToDb(list);
            return View(nameof(Index));
        }

        [HttpPost] // Wymaga instalacji Excela na serwrerze
        public ActionResult UploadMicrosoftOfficeInteropExcel(HttpPostedFileBase File)
        {
            var list = getListMicrosoftOfficeInteropExcel(File);
            AddToDb(list);
            return View(nameof(Index));
        }

        private List<ExcelModel> getListExcelDataReader(HttpPostedFileBase File)
        {
            List<ExcelModel> model = new List<ExcelModel>();
            try
            {

                using (IExcelDataReader dr = ExcelReaderFactory.CreateOpenXmlReader(File.InputStream))
                {
                    DataSet ds = dr.AsDataSet();
                    var sheet1 = ds.Tables["Sheet1"];

                    for (int i = 1; i < sheet1.Rows.Count; i++) //Pomijam pierwszy wiersz jako header
                    {
                        model.Add(new ExcelModel //Model może być dowolny, przykład jest dla różnych typów danych
                        {
                            //Id = 0, to pomijam- jest autonumerowanie w bazie
                            IntData = int.Parse(sheet1.Rows[i][0].ToString()),
                            StringData = sheet1.Rows[i][1].ToString(),
                            Date = DateTime.Parse(sheet1.Rows[i][2].ToString()),
                            DecimalData = decimal.Parse(sheet1.Rows[i][3].ToString()),
                            BoolData = int.Parse(sheet1.Rows[i][4].ToString()) == 1 ? true : false,
                        });
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                return model;
            }

        }
        private List<ExcelModel> getListMicrosoftOfficeInteropExcel(HttpPostedFileBase File)
        {
           
            try
            { 
                path = Server.MapPath("~/Content/" + File.FileName);
                File.SaveAs(path);
                xlApp = new Excel.Application();
                xlWorkbook = xlApp.Workbooks.Open(path);
                xlWorksheet = xlWorkbook.Sheets["Sheet1"];
                xlRange = xlWorksheet.UsedRange;
                List<ExcelModel> model = new List<ExcelModel>();
                for (int i = 2; i <= xlRange.Rows.Count; i++)
                {
                    model.Add(new ExcelModel //Model może być dowolny, przykład jest dla różnych typów danych
                    {
                        //Id = 0, to pomijam- jest autonumerowanie w bazie
                        IntData = int.Parse(xlRange.Cells[i, 1].Value2.ToString()),
                        StringData = xlRange.Cells[i, 2].Value2.ToString(),
                        Date = DateTime.FromOADate(double.Parse(xlRange.Cells[i, 3].Value2.ToString())),
                        DecimalData = decimal.Parse(xlRange.Cells[i, 4].Value2.ToString()),
                        BoolData = int.Parse(xlRange.Cells[i, 5].Value2.ToString()) == 1 ? true : false,
                    });
                }
                return model;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                try
                {
                    Marshal.ReleaseComObject(xlRange);
                }
                catch { }
                try
                {
                    Marshal.ReleaseComObject(xlWorksheet);
                    xlWorkbook.Close(false);
                }
                catch { }
                try
                {
                    Marshal.ReleaseComObject(xlWorkbook);
                }
                catch { }
                try
                {
                    xlApp.Quit();
                    Marshal.ReleaseComObject(xlApp);
                }
                catch { }
                System.IO.File.Delete(path);
            }
        }
    }
}