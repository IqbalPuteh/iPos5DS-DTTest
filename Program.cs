using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.Input;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.AutomationElements;
using Serilog;
using System.Threading;



namespace iPOSv5_DTTest // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static Application appx;
        //static Window AuAppMainWindow;
        static UIA3Automation automationUIA3;
        static ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());
        static AutomationElement window;
        static string dtID = ConfigurationManager.AppSettings["dtID"];
        static string dtName = ConfigurationManager.AppSettings["dtName"];
        static string LoginId = ConfigurationManager.AppSettings["loginId"];
        static string LoginPassword = ConfigurationManager.AppSettings["password"];
        static string appExe = ConfigurationManager.AppSettings["erpappnamepath"];
        static string dbserveraddr = ConfigurationManager.AppSettings["dbserveraddress"].ToUpper();
        static string issandbox = ConfigurationManager.AppSettings["uploadtosandbox"].ToUpper();
        static string enableconsolelog = ConfigurationManager.AppSettings["enableconsolelog"].ToUpper();
        static string appfolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\" + ConfigurationManager.AppSettings["appfolder"];
        static string uploadfolder = appfolder + @"\" + ConfigurationManager.AppSettings["uploadfolder"];
        static string sharingfolder = appfolder + @"\" + ConfigurationManager.AppSettings["sharingfolder"];
        static string iposgudang = ConfigurationManager.AppSettings["namagudangdiipos"].ToUpper();
        static string shortcuttoipos = ConfigurationManager.AppSettings["shortcuttoipos"].ToUpper();
        static string dbname = ConfigurationManager.AppSettings["dbname"];
        //static string screenshotfolder = appfolder + @"\" + ConfigurationManager.AppSettings["screenshotfolder"];
        static string logfilename = "";
        static int pid = 0;

        enum reportType
        {
            salesreport,
            arreport,
            masteroutletreport
        }

        [DllImport("user32.dll")]

        public static extern bool BlockInput(bool fBlockIt);

        private static AutomationElement WaitForElement(Func<AutomationElement> findElementFunc)
        {
            AutomationElement element = null;
            for (int i = 0; i < 2000; i++)
            {
                element = findElementFunc();
                if (element is not null)
                {
                    break;
                }

                //Thread.Sleep(1);
            }
            return element;
        }

        static bool IsFileExists(string path, string fileName)
        {
            string fullPath = Path.Combine(path, fileName);
            return File.Exists(fullPath);
        }

        static void Main(string[] args)
        {
            try
            {
                //* Call this method to disable keyboard input
                int maxWidth = Console.LargestWindowWidth;
                Console.SetWindowPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"");
                Console.WriteLine($"******************************************************************");
                Console.WriteLine($"                    Automasi akan dimulai !                       ");
                Console.WriteLine($"             Keyboard dan Mouse akan di matikan...                ");
                Console.WriteLine($"     Komputer akan menjalankan oleh applikasi robot automasi...   ");
                Console.WriteLine($" Aktifitas penggunakan komputer akan ter-BLOKIR sekitar 10 menit. ");
                Console.WriteLine($"******************************************************************");

#if DEBUG
                BlockInput(false);
#else
                BlockInput(true);
#endif
                var myFileUtil = new MyDirectoryManipulator();
                if (!Directory.Exists(appfolder))
                {
                    myFileUtil.CreateDirectory(appfolder);
                    myFileUtil.CreateDirectory(uploadfolder);
                    myFileUtil.CreateDirectory(sharingfolder);
                }
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.BackgroundColor = ConsoleColor.Black;
                var temp = myFileUtil.DeleteFiles(appfolder, MyDirectoryManipulator.FileExtension.Excel);
                Task.Run(() => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")} INF] {temp}"));
                temp = myFileUtil.DeleteFiles(appfolder, MyDirectoryManipulator.FileExtension.Log);
                Task.Run(() => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")} INF] {temp}"));
                temp = myFileUtil.DeleteFiles(appfolder, MyDirectoryManipulator.FileExtension.Zip);
                Task.Run(() => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")} INF] {temp}"));
                var config = new LoggerConfiguration();
                logfilename = "DEBUG-" + dtID + "-" + dtName + ".log";
                config.WriteTo.File(appfolder + Path.DirectorySeparatorChar + logfilename);
                if (enableconsolelog == "Y")
                {
                    config.WriteTo.Console();
                }
                Log.Logger = config.CreateLogger();

                Log.Information("iPOS ver.5 Automation - *** Started *** ");
                automationUIA3 = new UIA3Automation();
                window = automationUIA3.GetDesktop();

                if (!OpenAppAndDBConfig())
                {
                    Console.Beep();
                    Task.Delay(500);
                    Log.Information("application automation failed when running app (OpenAppAndDBConfig) !!!");
                    return;
                }
                if (!LoginApp())
                {
                    Console.Beep();
                    Task.Delay(500);
                    Log.Information("application automation failed when running app (LoginApp) !!!");
                    return;
                }
                if (!OpenReportParam("sales"))
                {
                    Console.Beep();
                    Task.Delay(500);
                    Log.Information("Application automation failed when running app (OpenReportParam) !!!");
                    return;
                }
                /*if (!SendingRptParam("sales"))
                {
                    Console.Beep();
                    Task.Delay(500);
                    Log.Information("Application automation failed when running app (SendingReportParam) !!!");
                    return;
                } */
            }
            catch (Exception ex)
            {
                Log.Information($"iPos v5 automation error => {ex.ToString()}");
                Task.Run(() => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm")} INF] iPos automation error => {ex.ToString()}"));
            }
            finally
            {
                Console.Beep();
                Task.Delay(500);
                Console.Beep();
                Task.Delay(500);
                //* Call this method to enable keyboard input
                BlockInput(false);

                Task.Run(() => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")} INF] iPOS ver.4 Automation - ***   END   ***"));
                if (automationUIA3 is not null)
                {
                    automationUIA3.Dispose();
                }
                Log.CloseAndFlush();
            }
        }

        private static string CheckingEle(AutomationElement? ele, int steps, string functionname)
        {
            var value = ele is null ? $"Automation error on #{steps} in function {functionname}..." : $"";
            return value;
        }

        private static bool MouseClickaction(AutomationElement ele)
        {
            try
            {
                var elecornerpos = ele.GetClickablePoint();
                Mouse.MoveTo(elecornerpos.X + 2, elecornerpos.Y + 2);
                Mouse.Click();
                return true;
            }
            catch (Exception ex)
            {
                Log.Information($"Error when executing mouse click action on element {ele.AutomationId} => {ex.Message}");
                return false;
            }
        }

        static bool OpenAppAndDBConfig()
        {
            var functionname = "OpenAppAndDBConfig";
            int step = 0;
            try
            {

                // Specify the path to your shortcut
                string shortcutPath = $@"{shortcuttoipos}";
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = shortcutPath;
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = false;

                Process process = new Process();
                process.StartInfo = startInfo;
                automationUIA3 = new UIA3Automation();

                try
                {
                    appx = Application.Launch(process.StartInfo);
                    window = appx.GetMainWindow(automationUIA3);
                    pid = appx.ProcessId;
                    Thread.Sleep(30000);
                }
                catch { Log.Information($"[{functionname}] Error ketika mebuka mmnghandle iPos window process..."); return false; }

                //* Picking Koneksi Database main window
                var checkingele = "";
                var ParentEle = window.FindFirstDescendant(cf => cf.ByName("Koneksi Database"));
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ParentEle.SetForeground();

                var ele = ParentEle.FindFirstChild(cf => cf.ByAutomationId("butServer", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.SetForeground();
                //* check coordinates and try mouse click on the coordinates
                ele.AsButton().Click();
                Thread.Sleep(2000);

                //frmServerLst
                ele = ParentEle.FindFirstChild(cf => cf.ByAutomationId("frmServerLst", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.SetForeground();
                //* check coordinates and try mouse click on the coordinates
                Thread.Sleep(2000);

                ele = window.FindFirstDescendant(cf => cf.ByName("Row 2"));
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.Click();
                Thread.Sleep(2000);

                ele = ParentEle.FindFirstDescendant(cf => cf.ByAutomationId("butPilih"));
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.Click();
                Thread.Sleep(2000);

                //tNamaDBCari
                ele = ParentEle.FindFirstChild(cf => cf.ByAutomationId("tNamaDBCari", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.AsTextBox().Text = dbname;
                Thread.Sleep(1000);

                //* check coordinates and try mouse click on the coordinates
                ele = ParentEle.FindFirstChild(cf => cf.ByAutomationId("tNamaDBCari", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.AsTextBox().Text = dbname;
                Thread.Sleep(1000);

                //grdListDB
                ele = ParentEle.FindFirstChild(cf => cf.ByAutomationId("grdListDB", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                MouseClickaction(ele);
                Thread.Sleep(10000);

                //automationUIA3 = new UIA3Automation();
                //window = automationUIA3.GetDesktop();
                //AutomationElement Parentele2 = null;
                //AutomationElement[] auEle = window.FindAllChildren (cr => cr.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));
                //foreach (AutomationElement item in auEle)
                //{
                //    if (item.Properties.ProcessId == pid)
                //    {
                //        Parentele2 = item;
                //        break;
                //    }
                //}
                //if (Parentele2 is null)
                //{
                //    Log.Information($"[Step #{step += 1}] Quitting, end of login automation function !!");
                //    return false;
                //}
                //Log.Information("Element Interaction on property named -> " + ele.Properties.Name.ToString());


                //ele = Parentele2.FindFirstChild(cr => cr.ByControlType(FlaUI.Core.Definitions.ControlType.List));
                //checkingele = CheckingEle(ele, step += 1, functionname);
                //if (checkingele != "") { Log.Information(checkingele); return false; }
                ////ele.Click();
                //Thread.Sleep(1000);

                //ele = ele.FindFirstChild(cf => cf.ByName(dbserveraddr));
                //checkingele = CheckingEle(ele, step += 1, functionname);
                //if (checkingele != "") { Log.Information(checkingele); return false; }
                ////ele.AsButton().Focus();
                ////Thread.Sleep(1000);
                //MouseClickaction(ele);
                //Thread.Sleep(2000);


                return true;
            }
            catch (Exception ex)
            {
                if (appx.ProcessId != null)
                {
                    appx.Close();
                }
                Log.Information($"Error when executing {functionname} => {ex.Message}");
                return false;

            }
        }

        private static bool LoginApp()
        {
            var functionname = "LoginApp";
            int step = 0;
            try
            {

                var checkingele = "";
                //* Picking form iPos 4 main windows
                automationUIA3 = new UIA3Automation();
                window = automationUIA3.GetDesktop();
                AutomationElement ParentEle = null;
                AutomationElement[] MainEle = window.FindAllChildren(cf => cf.ByName("iPos", PropertyConditionFlags.MatchSubstring));
                foreach (AutomationElement elem in MainEle)
                {
                    if (elem.Properties.ProcessId != pid)
                    {
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        ParentEle = elem; break;
                    }
                }
                //* Picking form login main window
                ParentEle = ParentEle.FindFirstChild(cf => cf.ByAutomationId("frmLogin"));
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ParentEle.SetForeground();
                Thread.Sleep(1000);

                //tUser
                var ele = ParentEle.FindFirstDescendant(cf => cf.ByAutomationId("tUser", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.AsTextBox().Enter(LoginId);
                Thread.Sleep(1000);

                //tPassword
                ele = ParentEle.FindFirstDescendant(cf => cf.ByAutomationId("tPassword", PropertyConditionFlags.None));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ele.AsTextBox().Enter(LoginPassword);
                Thread.Sleep(1000);

                ele = ParentEle.FindFirstChild(cf => cf.ByName("Masuk"));
                ele.AsButton().Focus();
                Thread.Sleep(1000);
                return MouseClickaction(ele);
            }
            catch (Exception ex)
            {
                Log.Information($"Error when executing {functionname} => {ex.Message}");
                return false;
            }
        }

        private static bool OpenReportParam(string reportname)
        {
            var functionname = "OpenReportParam -> " + reportname;
            int step = 0;
            try
            {
                //* Picking form iPos 4 main windows
                automationUIA3 = new UIA3Automation();
                window = automationUIA3.GetDesktop();
                var checkingele = "";
                AutomationElement ParentEle = null;
                AutomationElement[] MainEle = window.FindAllChildren(cf => cf.ByName("iPos", PropertyConditionFlags.MatchSubstring));
                foreach (AutomationElement elem in MainEle)
                {
                    if (!elem.Name.ToLower().Contains(LoginId.ToLower()))
                    {
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        ParentEle = elem; break;
                    }
                }
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }

                //Ribbon Tabs
                ParentEle = ParentEle.FindFirstDescendant(cf => cf.ByName("The Ribbon"));
                checkingele = CheckingEle(ParentEle, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ParentEle.SetForeground();

                //Ribbon Tabs
                var ele = ParentEle.FindFirstDescendant(cf => cf.ByName("Ribbon Tabs"));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                MouseClickaction(ele);
                Thread.Sleep(500);

                //Penjualan
                ele = ele.FindFirstChild(cf => cf.ByName("Laporan"));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }
                ParentEle.SetForeground();
                ele.AsTabItem().Select();
                ele.AsTabItem().Click();
                Thread.Sleep(1000);

                //Traversing to "Lower Ribbon" from Parent Element "The Ribbon"
                ele = ParentEle.FindFirstDescendant(cf => cf.ByName("Lower Ribbon"));
                checkingele = CheckingEle(ele, step += 1, functionname);
                if (checkingele != "") { Log.Information(checkingele); return false; }

                if (reportname == "sales")
                {
                    //'Penjualan' toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Penjualan && Konsinyasi"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }

                    //(This is) "Laporan Penjualan" toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Laporan Penjualan"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    ParentEle.SetForeground();
                    MouseClickaction(ele);
                    Thread.Sleep(1000);

                    //(This is) "Laporan Penjualan" button
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Laporan Penjualan"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    MouseClickaction(ele);
                }
                else if (reportname == "ar")
                {
                    //'Hutang Piutang' toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Hutang Piutang"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }

                    //(This is) 'Laporan Piutang' toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Laporan Piutang"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    ParentEle.SetForeground();
                    MouseClickaction(ele);
                    Thread.Sleep(1000);

                    //(This is) 'Laporan Pembayaran Piutang' button
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Laporan Pembayaran Piutang"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    MouseClickaction(ele);
                }
                else
                {
                    //'Master Data' toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Master"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }

                    //(This is) 'Laporan Master' toolbar
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Laporan Master"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    ParentEle.SetForeground();
                    MouseClickaction(ele);
                    Thread.Sleep(1000);

                    //(This is) 'aftar Pelanggan' button
                    ele = ele.FindFirstDescendant(cf => cf.ByName("Daftar Pelanggan"));
                    checkingele = CheckingEle(ele, step += 1, functionname);
                    if (checkingele != "") { Log.Information(checkingele); return false; }
                    MouseClickaction(ele);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Information($"Error when executing {functionname} => {ex.Message}");
                return false;
            }
        }




    }
}
            