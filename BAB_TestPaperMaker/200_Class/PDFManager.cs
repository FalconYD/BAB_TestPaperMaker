using ImageMagick;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BAB_TestPaperMaker
{
    public class PDFManager
    {
        public List<BitmapImage> m_listBitmap = new List<BitmapImage>();
        public double dQuality = 600;
        public int nPdfPageCount = 0;
        public Func<double, string, bool> delLoading = null;
        public Func<bool        > delLoaded = null;
        public int OpenPDF(string strPath)
        {
            int nRtn = 0;

            new Thread(new ParameterizedThreadStart(ConvertFileToImages)).Start(strPath);

            return nRtn;
        }
        //public void ConvertFileToImages(string filePath/*, string destinationPath*/)
        private void ConvertFileToImages(object obj)
        {
            string filePath = obj as string;
            if (filePath == null)
                return;
            try
            {
                m_listBitmap.Clear();
                MagickReadSettings magickReadSettings = new MagickReadSettings();
                magickReadSettings.Density = new Density(dQuality, dQuality);
                using (MagickImageCollection magickImageCollection = new MagickImageCollection())
                {
                    int page = 0;
                    delLoading?.Invoke(0, "File Loading...");
                    magickImageCollection.Read(filePath, magickReadSettings);
                    nPdfPageCount = magickImageCollection.Count;
                    delLoading?.Invoke(0, "File Loaded.");

                    foreach (MagickImage magickImage in magickImageCollection)
                    {
                        m_listBitmap.Add(ToBitmapImage(magickImage));
                        //magickImage.Format = MagickFormat.Png;
                        //string imageFilePath = string.Concat(destinationPath, "file-", page, ".png");
                        //magickImage.Write(imageFilePath);
                        page++;
                        delLoading?.Invoke((page / (double)nPdfPageCount) * 100, $"PDF Converting to Image... ( {page} / {nPdfPageCount} )");
                    }
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            delLoaded?.Invoke();
        }

        private BitmapImage ToBitmapImage(IMagickImage mimg, MagickFormat fmt = MagickFormat.Png24)
        {
            BitmapImage bmpSrc = new BitmapImage();
            using (MemoryStream ms = new MemoryStream())
            {
                mimg.Write(ms, fmt);
                ms.Position = 0;

                bmpSrc.BeginInit(); 
                bmpSrc.CacheOption = BitmapCacheOption.OnLoad; 
                bmpSrc.StreamSource = ms; 
                bmpSrc.EndInit();
                bmpSrc.Freeze();

                //bmp = (Bitmap)Bitmap.FromStream(ms);
            }

            return bmpSrc;
        }
    }
}
