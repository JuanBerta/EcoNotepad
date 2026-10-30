using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Tesseract;

namespace EcoNotepad
{
    /// <summary>
    /// Captures the primary screen (temporarily hiding the overlay form if it would
    /// cover the game tooltip) and runs Tesseract OCR on the captured image.
    /// </summary>
    public static class OcrService
    {
        private static readonly object Sync = new object();

        /// <summary>
        /// Captures the primary screen and returns the recognized text split by lines.
        /// </summary>
        public static List<string> RecognizeScreen(Form owner)
        {
            lock (Sync)
            {
                double savedOpacity = -1;
                bool lowered = false;
                if (owner != null && !owner.IsDisposed && owner.Visible && owner.Opacity > 0.02)
                {
                    savedOpacity = owner.Opacity;
                    owner.Opacity = 0.02;
                    lowered = true;
                    Thread.Sleep(200);
                }

                Bitmap bmp = null;
                try
                {
                    Rectangle bounds = Screen.PrimaryScreen.Bounds;
                    bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size,
                            CopyPixelOperation.SourceCopy);
                    }

                    using (Bitmap prepped = Preprocess(bmp))
                    {
                        return Recognize(prepped);
                    }
                }
                finally
                {
                    if (bmp != null) bmp.Dispose();
                    if (lowered && owner != null && !owner.IsDisposed)
                    {
                        owner.Opacity = savedOpacity;
                    }
                }
            }
        }

        /// <summary>Grayscale + 2x upscale to improve OCR accuracy.</summary>
        private static Bitmap Preprocess(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap dst = new Bitmap(w * 2, h * 2, PixelFormat.Format24bppRgb);

            BitmapData sd = src.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dd = dst.LockBits(new Rectangle(0, 0, w * 2, h * 2),
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int sStride = sd.Stride;
                int dStride = dd.Stride;
                unsafe
                {
                    byte* sp = (byte*)sd.Scan0.ToPointer();
                    byte* dp = (byte*)dd.Scan0.ToPointer();
                    for (int y = 0; y < h; y++)
                    {
                        byte* sRow = sp + (long)y * sStride;
                        byte* dRow0 = dp + (long)(y * 2) * dStride;
                        byte* dRow1 = dp + (long)(y * 2 + 1) * dStride;
                        for (int x = 0; x < w; x++)
                        {
                            byte b = sRow[x * 3 + 0];
                            byte g = sRow[x * 3 + 1];
                            byte r = sRow[x * 3 + 2];
                            byte gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                            int dx = x * 2;
                            dRow0[dx * 3 + 0] = gray;
                            dRow0[dx * 3 + 1] = gray;
                            dRow0[dx * 3 + 2] = gray;
                            dRow0[(dx + 1) * 3 + 0] = gray;
                            dRow0[(dx + 1) * 3 + 1] = gray;
                            dRow0[(dx + 1) * 3 + 2] = gray;
                            dRow1[dx * 3 + 0] = gray;
                            dRow1[dx * 3 + 1] = gray;
                            dRow1[dx * 3 + 2] = gray;
                            dRow1[(dx + 1) * 3 + 0] = gray;
                            dRow1[(dx + 1) * 3 + 1] = gray;
                            dRow1[(dx + 1) * 3 + 2] = gray;
                        }
                    }
                }
            }
            finally
            {
                src.UnlockBits(sd);
                dst.UnlockBits(dd);
            }
            return dst;
        }

        private static List<string> Recognize(Bitmap bmp)
        {
            string tessdata = FindTessdata();
            if (tessdata == null)
            {
                throw new FileNotFoundException(
                    "tessdata/eng.traineddata 未找到（已尝试 exe 目录与项目根目录）。");
            }

            var lines = new List<string>();
            using (var engine = new TesseractEngine(tessdata, "eng", EngineMode.Default))
            {
                using (Pix pix = PixConverter.ToPix(bmp))
                using (Page page = engine.Process(pix))
                {
                    string text = page.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        foreach (string line in text.Split(new[] { '\r', '\n' },
                            StringSplitOptions.RemoveEmptyEntries))
                        {
                            string t = line.Trim();
                            if (t.Length > 0) lines.Add(t);
                        }
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// Looks for a tessdata directory containing eng.traineddata, trying the
        /// exe directory first, then the project root.
        /// </summary>
        private static string FindTessdata()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string p1 = Path.Combine(exeDir, "tessdata");
            if (Directory.Exists(p1) && File.Exists(Path.Combine(p1, "eng.traineddata")))
                return p1;

            string projectRoot = FindProjectRoot();
            if (projectRoot != null)
            {
                string p2 = Path.Combine(projectRoot, "tessdata");
                if (Directory.Exists(p2) && File.Exists(Path.Combine(p2, "eng.traineddata")))
                    return p2;
            }
            return null;
        }

        private static string FindProjectRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "EcoNotepad.csproj")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
