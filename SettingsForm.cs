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

namespace Cliver.PdfDocumentParser
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();

            this.Icon = AssemblyRoutines.GetAppIcon();
            //Text = Application.ProductName;
            Text = AboutBox.AssemblyTitle;

            load_settings();
        }

        void load_settings()
        {
            AnchorMasterBoxColor.ForeColor = Settings.Appearance.AnchorMasterBoxColor;
            AnchorSecondaryBoxColor.ForeColor = Settings.Appearance.AnchorSecondaryBoxColor;
            SelectionBoxColor.ForeColor = Settings.Appearance.SelectionBoxColor;
            TableBoxColor.ForeColor = Settings.Appearance.TableBoxColor;

            SelectionBoxBorderWidth.Value = (decimal)Settings.Appearance.SelectionBoxBorderWidth;
            AnchorMasterBoxBorderWidth.Value = (decimal)Settings.Appearance.AnchorMasterBoxBorderWidth;
            AnchorSecondaryBoxBorderWidth.Value = (decimal)Settings.Appearance.SelectionBoxBorderWidth;
            TableBoxBorderWidth.Value = (decimal)Settings.Appearance.TableBoxBorderWidth;

            PdfPageImageResolution.Value = Settings.Constants.PdfPageImageResolution;
            CoordinateDeviationMargin.Value = (decimal)Settings.Constants.CoordinateDeviationMargin;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            try
            {
                Settings.Appearance.AnchorMasterBoxColor = AnchorMasterBoxColor.ForeColor;
                Settings.Appearance.AnchorSecondaryBoxColor = AnchorSecondaryBoxColor.ForeColor;
                Settings.Appearance.SelectionBoxColor = SelectionBoxColor.ForeColor;
                Settings.Appearance.TableBoxColor = TableBoxColor.ForeColor;

                Settings.Appearance.SelectionBoxBorderWidth = (float)SelectionBoxBorderWidth.Value;
                Settings.Appearance.AnchorMasterBoxBorderWidth = (float)AnchorMasterBoxBorderWidth.Value;
                Settings.Appearance.AnchorSecondaryBoxBorderWidth = (float)AnchorSecondaryBoxBorderWidth.Value;
                Settings.Appearance.TableBoxBorderWidth = (float)TableBoxBorderWidth.Value;

                Settings.Appearance.Save();

                Settings.Constants.PdfPageImageResolution = (int)PdfPageImageResolution.Value;
                Settings.Constants.CoordinateDeviationMargin = (float)CoordinateDeviationMargin.Value;

                Settings.Constants.Save();

                Close();
            }
            catch (Exception ex)
            {
                Message.Error2(ex);
            }
            Settings.Appearance.Reload();
            Settings.Constants.Reload();
        }

        private void bReset_Click(object sender, EventArgs e)
        {
            Settings.Appearance.Reset();
            Settings.Constants.Reset();
            load_settings();
        }

        private void About_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }
        
        private void SelectionBoxColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = Settings.Appearance.SelectionBoxColor;
            if (cd.ShowDialog() == DialogResult.OK)
                SelectionBoxColor.ForeColor = cd.Color;
        }

        private void AnchorMasterBoxColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = Settings.Appearance.AnchorMasterBoxColor;
            if (cd.ShowDialog() == DialogResult.OK)
                AnchorMasterBoxColor.ForeColor = cd.Color;
        }

        private void AnchorSecondaryBoxColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = Settings.Appearance.AnchorSecondaryBoxColor;
            if (cd.ShowDialog() == DialogResult.OK)
                AnchorSecondaryBoxColor.ForeColor = cd.Color;
        }
    }
}