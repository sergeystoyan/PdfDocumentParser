//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
using System.Text.RegularExpressions;
//using iTextSharp.text.pdf.parser;
using System.Drawing;

namespace Cliver.InvoiceParser
{
    public class Page : IDisposable
    {
        public Page(PageCollection pageCollection, int pageI)
        {
            this.pageCollection = pageCollection;
            this.pageI = pageI;
        }
        int pageI;
        PageCollection pageCollection;

        ~Page()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                    _bitmap = null;
                }
                if (_bitmapPreparedByTemplate != null)
                {
                    _bitmapPreparedByTemplate.Dispose();
                    _bitmapPreparedByTemplate = null;
                }
                if (_imageData != null)
                {
                    //imageData.Dispose();
                    _imageData = null;
                }
                if (_pdfCharBoxs != null)
                {
                    //charBoxLists.Dispose();
                    _pdfCharBoxs = null;
                }
                if (_ocrCharBoxs != null)
                {
                    _ocrCharBoxs = null;
                }
            }
        }

        public Bitmap Bitmap
        {
            get
            {
                if (_bitmap == null)
                    _bitmap = Pdf.RenderBitmap(pageCollection.PdfFile, pageI, Settings.General.PdfPageImageResolution);
                return _bitmap;
            }
        }
        Bitmap _bitmap;

        public void OnActiveTemplateUpdating(Settings.Template newTemplate)
        {
            if (pageCollection.ActiveTemplate.PagesRotation != newTemplate.PagesRotation || pageCollection.ActiveTemplate.AutoDeskew != newTemplate.AutoDeskew)
            {
                if (BitmapPreparedByTemplate != null)
                {
                    BitmapPreparedByTemplate.Dispose();
                    _bitmapPreparedByTemplate = null;
                }
                if (_ocrCharBoxs != null)
                    _ocrCharBoxs = null;
            }
            floatingAnchorIds2point0.Clear();
        }

        Dictionary<int, PointF?> floatingAnchorIds2point0 = new Dictionary<int, PointF?>();

        public void UncacheFloatingAnchor(int floatingAnchorId)
        {
            floatingAnchorIds2point0.Remove(floatingAnchorId);
        }

        public Bitmap GetRectangeFromBitmapPreparedByTemplate(float x, float y, float w, float h)
        {
            return BitmapPreparedByTemplate.Clone(new RectangleF(x, y, w, h), System.Drawing.Imaging.PixelFormat.Undefined);
            //return ImageRoutines.GetCopy(BitmapPreparedByTemplate, new RectangleF(x, y, w, h));
        }
        public Bitmap BitmapPreparedByTemplate
        {
            get
            {
                if (_bitmapPreparedByTemplate == null)
                {
                    Bitmap b;
                    if (pageCollection.ActiveTemplate.PagesRotation == Settings.Template.PageRotations.NONE && !pageCollection.ActiveTemplate.AutoDeskew)
                        b = Bitmap;
                    else
                    {
                        b = Bitmap.Clone(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), System.Drawing.Imaging.PixelFormat.Undefined);
                        //b = ImageRoutines.GetCopy(Bitmap);
                        switch (pageCollection.ActiveTemplate.PagesRotation)
                        {
                            case Settings.Template.PageRotations.NONE:
                                break;
                            case Settings.Template.PageRotations.Clockwise90:
                                b.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case Settings.Template.PageRotations.Clockwise180:
                                b.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                break;
                            case Settings.Template.PageRotations.Clockwise270:
                                b.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;
                            default:
                                throw new Exception("Unknown option: " + pageCollection.ActiveTemplate.PagesRotation);
                        }
                        if (pageCollection.ActiveTemplate.AutoDeskew)
                        {
                            using (ImageMagick.MagickImage image = new ImageMagick.MagickImage(b))
                            {
                                //image.Density = new PointD(600, 600);
                                //image.AutoLevel();
                                //image.Negate();
                                //image.AdaptiveThreshold(10, 10, new ImageMagick.Percentage(20));
                                //image.Negate();
                                image.Deskew(new ImageMagick.Percentage(10));
                                //image.AutoThreshold(AutoThresholdMethod.OTSU);
                                //image.Despeckle();
                                //image.WhiteThreshold(new Percentage(20));
                                //image.Trim();
                                b = image.ToBitmap();
                            }
                        }
                    }
                    _bitmapPreparedByTemplate = b;
                }
                return _bitmapPreparedByTemplate;
            }
        }
        Bitmap _bitmapPreparedByTemplate = null;

        public PointF? GetFloatingAnchorPoint0(int floatingAnchorId)
        {
            PointF? p;
            if (!floatingAnchorIds2point0.TryGetValue(floatingAnchorId, out p))
            {
                List < RectangleF > rs = FindFloatingAnchor(pageCollection.ActiveTemplate.FloatingAnchors.Find(a=>a.Id == floatingAnchorId));
                if (rs == null || rs.Count < 1)
                    p = null;
                else
                    p = new PointF(rs[0].X, rs[0].Y);
                floatingAnchorIds2point0[floatingAnchorId] = p;
            }
            return p;
        }

        public List<RectangleF> FindFloatingAnchor(Settings.Template.FloatingAnchor fa)
        {
            if (fa == null || fa.GetValue() == null)
                return null;

            switch (fa.ValueType)
            {
                case Settings.Template.ValueTypes.PdfText:
                    {
                        List<Settings.Template.FloatingAnchor.PdfTextValue.CharBox> ses = ((Settings.Template.FloatingAnchor.PdfTextValue)fa.GetValue()).CharBoxs;
                        if (ses.Count < 1)
                            return null;
                        List<Pdf.CharBox> bts = new List<Pdf.CharBox>();
                        foreach (Pdf.CharBox bt0 in PdfCharBoxs.Where(a => a.Char == ses[0].Char))
                        {
                            bts.Clear();
                            bts.Add(bt0);
                            for (int i = 1; i < ses.Count; i++)
                            {
                                float x = bt0.R.X + ses[i].Rectangle.X - ses[0].Rectangle.X;
                                float y = bt0.R.Y + ses[i].Rectangle.Y - ses[0].Rectangle.Y;
                                foreach (Pdf.CharBox bt in PdfCharBoxs.Where(a => a.Char == ses[i].Char))
                                {
                                    if (Math.Abs(bt.R.X - x) > Settings.General.CoordinateDeviationMargin)
                                        continue;
                                    if (Math.Abs(bt.R.Y - y) > Settings.General.CoordinateDeviationMargin)
                                        continue;
                                    if (bts.Contains(bt))
                                        continue;
                                    bts.Add(bt);
                                }
                            }
                            if (bts.Count == ses.Count)
                                return bts.Select(x => x.R).ToList();
                        }
                    }
                    return null;
                case Settings.Template.ValueTypes.OcrText:
                    {
                        List<Settings.Template.FloatingAnchor.OcrTextValue.CharBox> ses = ((Settings.Template.FloatingAnchor.OcrTextValue)fa.GetValue()).CharBoxs;
                        if (ses.Count < 1)
                            return null;
                        List<Ocr.CharBox> bts = new List<Ocr.CharBox>();
                        foreach (Ocr.CharBox bt0 in OcrCharBoxs.Where(a => a.Char == ses[0].Char))
                        {
                            bts.Clear();
                            bts.Add(bt0);
                            for (int i = 1; i < ses.Count; i++)
                            {
                                float x = bt0.R.X + ses[i].Rectangle.X - ses[0].Rectangle.X;
                                float y = bt0.R.Y + ses[i].Rectangle.Y - ses[0].Rectangle.Y;
                                foreach (Ocr.CharBox bt in OcrCharBoxs.Where(a => a.Char == ses[i].Char))
                                {
                                    if (Math.Abs(bt.R.X - x) > Settings.General.CoordinateDeviationMargin)
                                        continue;
                                    if (Math.Abs(bt.R.Y - y) > Settings.General.CoordinateDeviationMargin)
                                        continue;
                                    if (bts.Contains(bt))
                                        continue;
                                    bts.Add(bt);
                                }
                            }
                            if (bts.Count == ses.Count)
                                return bts.Select(x => x.R).ToList();
                        }
                    }
                    return null;
                case Settings.Template.ValueTypes.ImageData:
                    List<Settings.Template.FloatingAnchor.ImageDataValue.ImageBox> ibs = ((Settings.Template.FloatingAnchor.ImageDataValue)fa.GetValue()).ImageBoxs;
                    if (ibs.Count < 1)
                        return null;
                    PointF? p0 = ibs[0].ImageData.FindWithinImage(ImageData, pageCollection.ActiveTemplate.BrightnessTolerance, pageCollection.ActiveTemplate.DifferentPixelNumberTolerance, pageCollection.ActiveTemplate.FindBestImageMatch);
                    if (p0 == null)
                        return null;
                    List<RectangleF> rs = new List<RectangleF>();
                    rs.Add(new RectangleF((PointF)p0, new SizeF(ibs[0].Rectangle.Width, ibs[0].Rectangle.Height)));
                    PointF point0 = (PointF)p0;
                    for (int i = 1; i < ibs.Count; i++)
                    {
                        Settings.Template.RectangleF r = new Settings.Template.RectangleF(ibs[i].Rectangle.X + point0.X, ibs[i].Rectangle.Height + point0.Y, ibs[i].Rectangle.Width, ibs[i].Rectangle.Height);
                        if (!ibs[i].ImageData.ImageIsSimilar(new ImageData(GetRectangeFromBitmapPreparedByTemplate(r.X, r.Y, r.Width, r.Height)), pageCollection.ActiveTemplate.BrightnessTolerance, pageCollection.ActiveTemplate.DifferentPixelNumberTolerance))
                            return null;
                        rs.Add(r.GetSystemRectangleF());
                    }
                    return rs;
                default:
                    throw new Exception("Unknown option: " + fa.ValueType);
            }
        }

        //bool findFloatingAnchorSecondaryElements(PointF point0, Settings.Template.FloatingAnchor fa)
        //{
        //    for (int i = 1; i < fa.Elements.Count; i++)
        //    {
        //        if (!findFloatingAnchorElement(point0, fa.Elements[i]))
        //            return false;
        //    }
        //    return true;
        //}
        //bool findFloatingAnchorElement(PointF point0, Settings.Template.FloatingAnchor.Element e)
        //{
        //    switch (e.ElementType)
        //    {
        //        case Settings.Template.ValueTypes.PdfText:
        //            {
        //                List<Settings.Template.FloatingAnchor.PdfTextElement.CharBox> ses = ((Settings.Template.FloatingAnchor.PdfTextElement)e.Get()).PdfCharBoxs;
        //                List<Pdf.CharBox> bts = new List<Pdf.CharBox>();

        //                bts.Clear();
        //                for (int i = 0; i < ses.Count; i++)
        //                {
        //                    float x = point0.X + ses[i].Rectangle.X - ses[0].Rectangle.X;
        //                    float y = point0.Y + ses[i].Rectangle.Y - ses[0].Rectangle.Y;
        //                    foreach (Pdf.CharBox bt in charBoxLists.Where(a => a.Text == ses[i].Char))
        //                    {
        //                        if (Math.Abs(bt.R.X - x) > Settings.General.CoordinateDeviationMargin)
        //                            continue;
        //                        if (Math.Abs(bt.R.Y - y) > Settings.General.CoordinateDeviationMargin)
        //                            continue;
        //                        if (bts.Contains(bt))
        //                            continue;
        //                        bts.Add(bt);
        //                    }
        //                }
        //                return bts.Count == ses.Count;
        //            }
        //        case Settings.Template.ValueTypes.OcrText:
        //            {
        //                return true;
        //            }
        //        case Settings.Template.ValueTypes.ImageData:
        //            {
        //                ImageData id = (ImageData)e.Get();
        //                return id.ImageIsSimilar(new ImageData(GetRectangeFromBitmapPreparedByTemplate(new Settings.Template.RectangleF(point0.X, point0.Y, id.Width, id.Height))));
        //            }
        //        default:
        //            throw new Exception("Unknown option: " + e.ElementType);
        //    }
        //}

        public ImageData ImageData
        {
            get
            {
                if (_imageData == null)
                    _imageData = new ImageData(BitmapPreparedByTemplate);
                return _imageData;
            }
        }
        ImageData _imageData;

        public List<Pdf.CharBox> PdfCharBoxs
        {
            get
            {
                if (_pdfCharBoxs == null)
                    _pdfCharBoxs = Pdf.GetCharBoxsFromPage(pageCollection.PdfReader, pageI);
                return _pdfCharBoxs;
            }
        }
        List<Pdf.CharBox> _pdfCharBoxs;

        public List<Ocr.CharBox> OcrCharBoxs
        {
            get
            {
                if (_ocrCharBoxs == null)
                {
                    _ocrCharBoxs = Ocr.This.GetCharBoxs(BitmapPreparedByTemplate);
                }
                return _ocrCharBoxs;
            }
        }
        List<Ocr.CharBox> _ocrCharBoxs;

        public bool IsInvoiceFirstPage()
        {
            string error;
            return IsInvoiceFirstPage(out error);
        }

        public bool IsInvoiceFirstPage(out string error)
        {
            foreach (Settings.Template.Mark m in pageCollection.ActiveTemplate.InvoiceFirstPageRecognitionMarks)
            {
                object v = GetValue(m.FloatingAnchorId, m.Rectangle, m.ValueType, out error);
                switch (m.ValueType)
                {
                    case Settings.Template.ValueTypes.PdfText:
                        {
                            string t1 = FieldPreparation.Normalize(m.Value);
                            string t2 = FieldPreparation.Normalize((string)v);
                            if (t1 == t2)
                                break;
                                error = "InvoiceFirstPageRecognitionMark[" + pageCollection.ActiveTemplate.InvoiceFirstPageRecognitionMarks.IndexOf(m) + "]:\r\n" + t2 + "\r\n <> \r\n" + t1;
                                return false;
                        }
                    case Settings.Template.ValueTypes.OcrText:
                        {
                            string t1 = FieldPreparation.Normalize(m.Value);
                            string t2 = FieldPreparation.Normalize((string)v);
                            if (t1 == t2)
                                error = "InvoiceFirstPageRecognitionMark[" + pageCollection.ActiveTemplate.InvoiceFirstPageRecognitionMarks.IndexOf(m) + "]:\r\n" + t2 + "\r\n <> \r\n" + t1;
                            return false;
                        }
                    case Settings.Template.ValueTypes.ImageData:
                        {
                            ImageData id = ImageData.Deserialize(m.Value);
                            if (id.ImageIsSimilar((ImageData)(v), pageCollection.ActiveTemplate.BrightnessTolerance, pageCollection.ActiveTemplate.DifferentPixelNumberTolerance))
                                break;
                            error = "InvoiceFirstPageRecognitionMark[" + pageCollection.ActiveTemplate.InvoiceFirstPageRecognitionMarks.IndexOf(m) + "]: image is not similar.";
                            return false;
                        }
                    default:
                        throw new Exception("Unknown option: " + m.ValueType);
                }
            }
            error = null;
            return true;
        }

        public string GetFieldText(string fieldName)
        {
            Settings.Template.Field f = pageCollection.ActiveTemplate.Fields.Find(a => a.Name == fieldName);
            string error;
            object v = GetValue(f.FloatingAnchorId, f.Rectangle, f.ValueType, out error);
            if (v is ImageData)
                return ((ImageData)v).Serialize();
            return FieldPreparation.Normalize(prepareField((string)v));
        }

        public object GetValue(int? floatingAnchorId, Settings.Template.RectangleF r_, Settings.Template.ValueTypes valueType, out string error)
        {
            //try
            //{
            if(r_.Width <= Settings.General.CoordinateDeviationMargin || r_.Height <= Settings.General.CoordinateDeviationMargin)
            {
                error = "Rectangular is malformed.";
                return null;
            }
            PointF point0 = new PointF(0, 0);
            if (floatingAnchorId != null)
            {
                PointF? p0;
                p0 = GetFloatingAnchorPoint0((int)floatingAnchorId);
                if (p0 == null)
                {
                    error = "FloatingAnchor[" + floatingAnchorId + "] not found.";
                    return null;
                }
                point0 = (PointF)p0;
            }
            Settings.Template.RectangleF r = new Settings.Template.RectangleF(r_.X + point0.X, r_.Y + point0.Y, r_.Width, r_.Height);
            error = null;
            switch (valueType)
            {
                case Settings.Template.ValueTypes.PdfText:
                    return Pdf.GetTextByTopLeftCoordinates(PdfCharBoxs, r.X, r.Y, r.Width, r.Height);
                case Settings.Template.ValueTypes.OcrText:
                    return Ocr.This.GetText(BitmapPreparedByTemplate, r.X / Settings.General.Image2PdfResolutionRatio, r.Y / Settings.General.Image2PdfResolutionRatio, r.Width / Settings.General.Image2PdfResolutionRatio, r.Height / Settings.General.Image2PdfResolutionRatio);
                case Settings.Template.ValueTypes.ImageData:
                    return new ImageData(GetRectangeFromBitmapPreparedByTemplate(r.X / Settings.General.Image2PdfResolutionRatio, r.Y / Settings.General.Image2PdfResolutionRatio, r.Width / Settings.General.Image2PdfResolutionRatio, r.Height / Settings.General.Image2PdfResolutionRatio));
                default:
                    throw new Exception("Unknown option: " + valueType);
            }
            //}
            //catch(Exception e)
            //{
            //    error = Log.GetExceptionMessage(e);
            //}
            //return null;
        }
        static Dictionary<Bitmap, ImageData> bs2id = new Dictionary<Bitmap, ImageData>();

        Dictionary<string, string> fieldNames2texts = new Dictionary<string, string>();

        public float Height;

        static string prepareField(string f)
        {
            return Regex.Replace(f, @"\-", "");
        }
    }
}