//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace Cliver.InvoiceParser
/*


*/
{/*TBD:
    - settings panel
    - headers management: dialog with moving columns, with lists of templates having this header
- template editor: resolution, drop-down 'rotate', auto-deskew,auto-crop
    - image rotation
    - to cliverbot
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
                LogMessage.Error(e);
                Environment.Exit(0);
            };

            Message.TopMost = true;

            Config.Reload();
            LogMessage.DisableStumblingDialogs = false;
            Log.ShowDeleteOldLogsDialog = false;
            Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir, true);
        }

        [STAThread]
        static void Main()
        {
            try
            {
                MainForm mf = new MainForm();
                Application.Run(mf);
            }
            catch (Exception e)
            {
                Message.Error(e);
            }
            Environment.Exit(0);
        }
    }
}