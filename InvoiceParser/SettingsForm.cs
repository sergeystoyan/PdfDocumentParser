﻿//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cliver.PdfDocumentParser;
using System.IO;

namespace Cliver.InvoiceParser
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();

            this.Icon = AssemblyRoutines.GetAppIcon();
            Text = Application.ProductName;

            load_settings();
        }

        void load_settings()
        {
            IgnoreHidddenFiles.Checked = Settings.General.IgnoreHidddenFiles;
            ReadInputFolderRecursively.Checked = Settings.General.ReadInputFolderRecursively;

            if (!string.IsNullOrWhiteSpace(Settings.Templates.__File) && File.Exists(Settings.Templates.__File))
                //currentTemplatesTime.Text = "Current templates timestamp: " + File.GetLastWriteTime(Settings.Templates.__File).ToString();
                LastDownloadedTemplatesTimestamp.Text = Settings.Remote.LastDownloadedTemplatesTimestamp.ToString();
            else
                LastDownloadedTemplatesTimestamp.Text = "-- empty --";
            RemoteAccessToken.Text = Settings.Remote.AccessToken;
            UpdateTemplatesOnStart.Checked = Settings.Remote.UpdateTemplatesOnStart;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            try
            {
                Settings.General.IgnoreHidddenFiles = IgnoreHidddenFiles.Checked;
                Settings.General.ReadInputFolderRecursively = ReadInputFolderRecursively.Checked;

                Settings.Remote.AccessToken = RemoteAccessToken.Text;
                Settings.Remote.UpdateTemplatesOnStart = UpdateTemplatesOnStart.Checked;

                Settings.General.Save();
                Settings.General.Reload();

                Settings.Remote.Save();
                Settings.Remote.Reload();

                Close();
            }
            catch (Exception ex)
            {
                Message.Error2(ex);
            }
        }

        private void bReset_Click(object sender, EventArgs e)
        {
            Settings.General.Reset();
            Settings.Remote.Reset();
            PdfDocumentParser.Settings.Appearance.Reset();
            PdfDocumentParser.Settings.ImageProcessing.Reset();
            load_settings();
        }

        private void updateTemplates_Click(object sender, EventArgs e)
        {
            Settings.Remote.AccessToken = RemoteAccessToken.Text;
            TemplatesUpdatingForm.StartUpdatingTemplatesFromRemoteLocation(false, this, () =>
            {
                this.BeginInvoke(() => { LastDownloadedTemplatesTimestamp.Text = Settings.Remote.LastDownloadedTemplatesTimestamp.ToString(); });
                MainForm.This.LoadTemplates();
            });
        }

        //private void bResetTemplates_Click(object sender, EventArgs e)
        //{
        //    if (!Message.YesNo("The templates will be reset to the initial state. Proceed?"))
        //        return;
        //    Settings.Templates.Reset();
        //    Settings.Templates.Save();
        //    ProcessRoutines.Restart();
        //}
    }
}
