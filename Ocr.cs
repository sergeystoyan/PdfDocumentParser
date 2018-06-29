﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Tesseract;

namespace Cliver.InvoiceParser
{
    public  class Ocr : IDisposable
    {
        public static Ocr This = new Ocr();

        ~Ocr()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_engine != null)
            {
                _engine.Dispose();
                _engine = null;
            }
        }

        Tesseract.TesseractEngine engine
        {
            get
            {
                if (_engine == null)
                    _engine = new Tesseract.TesseractEngine(@"./tessdata", "eng", Tesseract.EngineMode.Default);
                return _engine;
            }
        }
        Tesseract.TesseractEngine _engine = null;

        public string GetText(Bitmap b, float x, float y, float w, float h)
        {
            Rectangle r = new Rectangle((int)x, (int)y, (int)w, (int)h);
            r.Intersect(new Rectangle(0, 0, b.Width, b.Height));
            if (Math.Abs(r.Width) < Settings.General.CoordinateDeviationMargin || Math.Abs(r.Height) < Settings.General.CoordinateDeviationMargin)
                return null;
            using (var page = engine.Process(b, new Rect(r.X, r.Y, r.Width, r.Height), PageSegMode.SingleBlock))
            {
                return page.GetText();
            }
        }

        public List<CharBox> GetCharBoxs(Bitmap b)
        {
            List<CharBox> cbs = new List<CharBox>();
            using (var page = engine.Process(b, PageSegMode.Auto))
            {
                //string t = page.GetHOCRText(1, true);
                //var dfg = page.GetThresholdedImage();                        
                //Tesseract.Orientation o;
                //float c;
                // page.DetectBestOrientation(out o, out c);
                //  var l = page.AnalyseLayout();
                //var ti =   l.GetBinaryImage(Tesseract.PageIteratorLevel.Para);
                //Tesseract.Rect r;
                // l.TryGetBoundingBox(Tesseract.PageIteratorLevel.Block, out r);
                using (var i = page.GetIterator())
                {
                    //int j = 0;
                    //i.Begin();
                    //do
                    //{
                    //    bool g = i.IsAtBeginningOf(Tesseract.PageIteratorLevel.Block);
                    //    bool v = i.TryGetBoundingBox(Tesseract.PageIteratorLevel.Block, out r);
                    //    var bt = i.BlockType;
                    //    //if (Regex.IsMatch(bt.ToString(), @"image", RegexOptions.IgnoreCase))
                    //    //{
                    //    //    //i.TryGetBoundingBox(Tesseract.PageIteratorLevel.Block,out r);
                    //    //    Tesseract.Pix p = i.GetBinaryImage(Tesseract.PageIteratorLevel.Block);
                    //    //    Bitmap b = Tesseract.PixConverter.ToBitmap(p);
                    //    //    b.Save(Log.AppDir + "\\test" + (j++) + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    //    //}
                    //} while (i.Next(Tesseract.PageIteratorLevel.Block));
                    do
                    {
                        do
                        {
                            do
                            {
                                do
                                {
                                    //if (i.IsAtBeginningOf(PageIteratorLevel.Block))
                                    //{
                                    //}
                                    //if (i.IsAtBeginningOf(PageIteratorLevel.Para))
                                    //{
                                    //}
                                    //if (i.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                    //{
                                    //}
                                    //if (i.IsAtBeginningOf(PageIteratorLevel.Word))
                                    //{
                                    //}

                                    Rect r;
                                    if (i.TryGetBoundingBox(PageIteratorLevel.Symbol, out r))
                                        cbs.Add(new CharBox { Char = i.GetText(PageIteratorLevel.Symbol), R = new RectangleF(r.X1, r.Y1, r.Width, r.Height) });
                                } while (i.Next(PageIteratorLevel.Word, PageIteratorLevel.Symbol));
                            } while (i.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                        } while (i.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    } while (i.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                }
            }
            return cbs;
        }

        public class CharBox
        {
            public string Char;
            public System.Drawing.RectangleF R;
        }

        public static string GetTextByTopLeftCoordinates(List<CharBox> bts, float x, float y, float w, float h)
        {
            System.Drawing.RectangleF d = new System.Drawing.RectangleF { X = x, Y = y, Width = w, Height = h };
            bts = RemoveDuplicatesAndOrder(bts.Where(a => (d.Contains(a.R) /*|| d.IntersectsWith(a.R)*/)));
            StringBuilder sb = new StringBuilder(bts.Count > 0 ? bts[0].Char : "");
            for (int i = 1; i < bts.Count; i++)
            {
                if (Math.Abs(bts[i - 1].R.Y - bts[i].R.Y) > bts[i - 1].R.Height / 2)
                    sb.Append("\r\n");
                else if (Math.Abs(bts[i - 1].R.Right - bts[i].R.X) > Math.Min(bts[i - 1].R.Width, bts[i].R.Width) / 2)
                    sb.Append(" ");
                sb.Append(bts[i].Char);
            }
            return sb.ToString();
        }

        public static List<CharBox> RemoveDuplicatesAndOrder(IEnumerable<CharBox> bts)
        {
            List<CharBox> bs = bts.Where(a => a.R.Width >= 0 && a.R.Height >= 0).ToList();//some symbols are duplicated with negative width anf height
            for (int i = 0; i < bs.Count; i++)
                for (int j = bs.Count - 1; j > i; j--)
                {
                    if (Math.Abs(bs[i].R.X - bs[j].R.X) > Settings.General.CoordinateDeviationMargin)//some symbols are duplicated in [almost] same position
                        continue;
                    if (Math.Abs(bs[i].R.Y - bs[j].R.Y) > Settings.General.CoordinateDeviationMargin)//some symbols are duplicated in [almost] same position
                        continue;
                    if (bs[i].Char != bs[j].Char)
                        continue;
                    bs.RemoveAt(j);
                }
            return bs.OrderBy(a => a.R.Y).OrderBy(a => a.R.X).ToList();
        }

        public static List<CharBox> GetCharBoxsSurroundedByRectangle(List<CharBox> bts, System.Drawing.RectangleF r)
        {
            return bts.Where(a => /*selectedR.IntersectsWith(a.R) || */r.Contains(a.R)).ToList();
        }
    }
}
