﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cliver.PdfDocumentParser
{
    public partial class FloatingAnchorPdfTextControl : UserControl
    {
        public FloatingAnchorPdfTextControl(Template.FloatingAnchor.PdfTextValue value)
        {
            InitializeComponent();

            Value = value;
        }

        public Template.FloatingAnchor.PdfTextValue Value
        {
            get
            {
                _value.PositionDeviationIsAbsolute = PositionDeviationIsAbsolute.Checked;
                return _value;
            }
            set
            {
                _value = value;
                if (value == null)
                    return;
                StringBuilder sb = new StringBuilder();
                foreach (var l in Pdf.RemoveDuplicatesAndGetLines(value.CharBoxs.Select(x => new Pdf.CharBox { Char = x.Char, R = x.Rectangle.GetSystemRectangleF() }), true))
                {
                    foreach (var cb in l.CharBoxes)
                        sb.Append(cb.Char);
                    sb.Append("\r\n");
                }
                text.Text = sb.ToString();

                PositionDeviationIsAbsolute.Checked = value.PositionDeviationIsAbsolute;
            }
        }
        Template.FloatingAnchor.PdfTextValue _value;

        //public bool Text
        //{
        //    get
        //    {
        //        return findBestImageMatch.Checked;
        //    }
        //    set
        //    {
        //        findBestImageMatch.Checked = value;
        //    }
        //}
    }
}