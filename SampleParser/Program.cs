//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Cliver.SampleParser
{
    /*
 TBD:

     */
    class Program
    {
        static Program()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                Exception e = (Exception)args.ExceptionObject;
                Win.LogMessage.Error(e);
                Environment.Exit(0);
            };

            Version = AssemblyRoutines.GetAssemblyCompiledTime(Assembly.GetEntryAssembly()).ToString("yyMMdd-HHmmss"); //String.Format("Version {0}", AssemblyVersion);
            Name = Application.ProductName;

            Message.TopMost = true;

            Config.Reload();
            Win.LogMessage.DisableStumblingDialogs = false;
            Log.DeleteOldLogsDialog = null;
            Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir, true);
        }
        public static readonly string Version;
        public static readonly string Name;

        [STAThread]
        static void Main()
        {
            try
            {
                PdfDocumentParser.Program.Initialize();

                PdfDocumentParser.TemplateWindow tw = new PdfDocumentParser.TemplateWindow();
                tw.ShowDialog();

                //Application.Run(MainForm.This);
            }
            catch (Exception e)
            {
                Message.Error(e);
            }
            Environment.Exit(0);
        }
    }
}