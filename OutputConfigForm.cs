﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cliver.InvoiceParser
{
    public partial class OutputConfigForm : Form
    {
        public OutputConfigForm()
        {
            InitializeComponent();

            Icon = AssemblyRoutines.GetAppIcon();
            Text = "Output Headers";

            List<string> fns2 = GetOrderedHeaders();
            outputHeaders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            List<string> templateNames = new List<string>();
            foreach (string fn in fns2)
            {
                int i = outputHeaders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = fn, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True, Alignment = DataGridViewContentAlignment.TopLeft, }, SortMode = DataGridViewColumnSortMode.NotSortable });
                templateNames.Add(string.Join("\r\n", Settings.Templates.Templates.Where(x => x.Active && x.Fields.Where(f => f.Name == fn).FirstOrDefault() != null).Select(x => x.Name)));
            }
            if (fns2.Count > 0)
            {
                int j = outputHeaders.Rows.Add(new DataGridViewRow());
                for (int i = 0; i < outputHeaders.Columns.Count; i++)
                    outputHeaders.Rows[j].Cells[i].Value = templateNames[i];
            }
        }

        public static List<string> GetOrderedHeaders()
        {
            List<Settings.Template> ts = Settings.Templates.Templates.Where(x => x.Active).ToList();

            List<string> fns = new List<string>();
            foreach (Settings.Template t in ts)
                foreach (Settings.Template.Field f in t.Fields)
                    if (!fns.Contains(f.Name))
                        fns.Add(f.Name);

            List<string> fns2 = new List<string>();
            for (int i = 0; i < Settings.General.OrderedOutputFieldNames.Count; i++)
            {
                string fn = Settings.General.OrderedOutputFieldNames[i];
                if (fns.Remove(fn))
                    fns2.Add(fn);
            }
            fns2.AddRange(fns);
            return fns2;
        }

        private void bOk_Click(object sender, EventArgs e)
        {
            Settings.General.OrderedOutputFieldNames.Clear();
            List<DataGridViewColumn> cs = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn c in outputHeaders.Columns)
                cs.Add(c);
            Settings.General.OrderedOutputFieldNames = cs.OrderBy(x => x.DisplayIndex).Select(x => x.HeaderText).ToList();
            Settings.General.Save();

            Close();
            DialogResult = DialogResult.OK;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.Cancel;
        }
    }
}