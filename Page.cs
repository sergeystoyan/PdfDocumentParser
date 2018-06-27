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
                if (_bitmapPreparedForTemplate != null)
                {
                    _bitmapPreparedForTemplate.Dispose();
                    _bitmapPreparedForTemplate = null;
                }
                if (_imageData != null)
                {
                    //imageData.Dispose();
                    _imageData = null;
                }
                if (_CharBoxs != null)
                {
                    //charBoxLists.Dispose();
                    _CharBoxs = null;
                }
            }
        }

        Bitmap bitmap
        {
            get
            {
                if (_bitmap == null)
                    _bitmap = Pdf.RenderBitmap(pageCollection.PdfFile, pageI, Settings.General.PdfPageImageResolution);
                return _bitmap;
            }
        }
        Bitmap _bitmap;

        //public Settings.Template pageCollection.ActiveTemplate
        //{
        //    set
        //    {
        //        if (_activeTemplate == value)
        //            return;
        //        if (_activeTemplate.PagesRotation != value.PagesRotation || _activeTemplate.AutoDeskew != value.AutoDeskew)
        //        {
        //            if (BitmapPreparedForTemplate != null)
        //            {
        //                BitmapPreparedForTemplate.Dispose();
        //                _bitmapPreparedForTemplate = null;
        //            }
        //        }
        //        floatingAnchorIds2point0.Clear();
        //        _activeTemplate = value;
        //    }
        //    get
        //    {
        //        return _activeTemplate;
        //    }
        //}
        //Settings.Template _activeTemplate;
        public void OnActiveTemplateUpdating(Settings.Template newTemplate)
        {
            if (pageCollection.ActiveTemplate == newTemplate)
                return;
            if (pageCollection.ActiveTemplate.PagesRotation != newTemplate.PagesRotation || pageCollection.ActiveTemplate.AutoDeskew != newTemplate.AutoDeskew)
            {
                if (BitmapPreparedForTemplate != null)
                {
                    BitmapPreparedForTemplate.Dispose();
                    _bitmapPreparedForTemplate = null;
                }
            }
            floatingAnchorIds2point0.Clear();
        }

        Dictionary<int, PointF?> floatingAnchorIds2point0 = new Dictionary<int, PointF?>();

        public void UncacheFloatingAnchor(int floatingAnchorId)
        {
            floatingAnchorIds2point0.Remove(floatingAnchorId);
        }

        public Bitmap GetRectangeFromBitmapPreparedForTemplate(float x, float y, float w, float h)
        {
            return BitmapPreparedForTemplate.Clone(new RectangleF(x, y, w, h), System.Drawing.Imaging.PixelFormat.Undefined);
        }
        public Bitmap BitmapPreparedForTemplate
        {
            get
            {
                if (_bitmapPreparedForTemplate == null)
                {
                    Bitmap b;
                    if (pageCollection.ActiveTemplate.PagesRotation == Settings.Template.PageRotations.NONE && !pageCollection.ActiveTemplate.AutoDeskew)
                        b = bitmap;
                    else
                    {
                        b = _bitmap.Clone(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), System.Drawing.Imaging.PixelFormat.Undefined);
                        //b = ImageRoutines.GetCopy(b);
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
                    _bitmapPreparedForTemplate = b;
                }
                return _bitmapPreparedForTemplate;
            }
        }
        Bitmap _bitmapPreparedForTemplate = null;

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
            if (fa == null)
                return null;

            switch (fa.ValueType)
            {
                case Settings.Template.ValueTypes.PdfText:
                    List<Settings.Template.FloatingAnchor.PdfTextElement.CharBox> ses = ((Settings.Template.FloatingAnchor.PdfTextElement)fa.Get()).CharBoxs;
                    if (ses.Count < 1)
                        return null;
                    List<Pdf.BoxText> bts = new List<Pdf.BoxText>();
                    foreach (Pdf.BoxText bt0 in CharBoxs.Where(a => a.Text == ses[0].Char))
                    {
                        bts.Clear();
                        bts.Add(bt0);
                        for (int i = 1; i < ses.Count; i++)
                        {
                            float x = bt0.R.X + ses[i].Rectangle.X - ses[0].Rectangle.X;
                            float y = bt0.R.Y + ses[i].Rectangle.Y - ses[0].Rectangle.Y;
                            foreach (Pdf.BoxText bt in CharBoxs.Where(a => a.Text == ses[i].Char))
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
                        {
                            //PointF point0 = new PointF(bts[0].R.X, bts[0].R.Y);
                            //if (findFloatingAnchorSecondaryElements(point0, fa))
                            //    return point0;
                            return bts.Select(x => x.R).ToList();
                        }
                    }
                    return null;
                case Settings.Template.ValueTypes.OcrText:
                    return null;
                case Settings.Template.ValueTypes.ImageData:
                    List<Settings.Template.FloatingAnchor.ImageDataElement.ImageBox> ibs = ((Settings.Template.FloatingAnchor.ImageDataElement)fa.Get()).ImageBoxs;
                    if (ibs.Count < 1)
                        return null;

                    PointF? p0 = ibs[0].ImageData.FindWithinImage(imageData);
                    if (p0 == null)
                        return null;
                    PointF point0 = (PointF)p0;

                    for (int i = 1; i < ibs.Count; i++)
                    {
                        Settings.Template.RectangleF r = new Settings.Template.RectangleF(ibs[i].Rectangle.X + point0.X, ibs[i].Rectangle.Height + point0.Y, ibs[i].Rectangle.Width, ibs[i].Rectangle.Height);
                        if (!ibs[i].ImageData.ImageIsSimilar(new ImageData(GetRectangeFromBitmapPreparedForTemplate(r.X, r.Y, r.Width, r.Height))))
                            return null;
                    }
                    return new List<RectangleF> { new RectangleF(point0.X, point0.Y, imageData.Width, imageData.Height) };
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
        //                List<Settings.Template.FloatingAnchor.PdfTextElement.CharBox> ses = ((Settings.Template.FloatingAnchor.PdfTextElement)e.Get()).CharBoxs;
        //                List<Pdf.BoxText> bts = new List<Pdf.BoxText>();

        //                bts.Clear();
        //                for (int i = 0; i < ses.Count; i++)
        //                {
        //                    float x = point0.X + ses[i].Rectangle.X - ses[0].Rectangle.X;
        //                    float y = point0.Y + ses[i].Rectangle.Y - ses[0].Rectangle.Y;
        //                    foreach (Pdf.BoxText bt in charBoxLists.Where(a => a.Text == ses[i].Char))
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
        //                return id.ImageIsSimilar(new ImageData(GetRectangeFromBitmapPreparedForTemplate(new Settings.Template.RectangleF(point0.X, point0.Y, id.Width, id.Height))));
        //            }
        //        default:
        //            throw new Exception("Unknown option: " + e.ElementType);
        //    }
        //}

        ImageData imageData
        {
            get
            {
                if (_imageData == null)
                    _imageData = new ImageData(bitmap);
                return _imageData;
            }
        }
        ImageData _imageData;

        public List<Pdf.BoxText> CharBoxs
        {
            get
            {
                if (_CharBoxs == null)
                    _CharBoxs = Pdf.GetCharBoxsFromPage(pageCollection.PdfReader, pageI);
                return _CharBoxs;
            }
        }
        List<Pdf.BoxText> _CharBoxs;

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
                            if (id.ImageIsSimilar((ImageData)(v)))
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
                        return Pdf.GetTextByTopLeftCoordinates(CharBoxs, r.X, r.Y, r.Width, r.Height);
                    case Settings.Template.ValueTypes.OcrText:
                        return TesseractW.This.GetText(BitmapPreparedForTemplate, r.X / Settings.General.Image2PdfResolutionRatio, r.Y / Settings.General.Image2PdfResolutionRatio, r.Width / Settings.General.Image2PdfResolutionRatio, r.Height / Settings.General.Image2PdfResolutionRatio);
                    case Settings.Template.ValueTypes.ImageData:
                        return new ImageData(GetRectangeFromBitmapPreparedForTemplate(r.X / Settings.General.Image2PdfResolutionRatio, r.Y / Settings.General.Image2PdfResolutionRatio, r.Width / Settings.General.Image2PdfResolutionRatio, r.Height / Settings.General.Image2PdfResolutionRatio));
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

        Dictionary<string, string> fieldNames2texts = new Dictionary<string, string>();

        public float Height;

        static string prepareField(string f)
        {
            return Regex.Replace(f, @"\-", "");
        }
    }
}