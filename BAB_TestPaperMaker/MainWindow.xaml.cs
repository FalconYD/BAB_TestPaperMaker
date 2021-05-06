using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls.Ribbon;
using FalconWpf;
using System.IO;
using System.Windows.Interop;

namespace BAB_TestPaperMaker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        PDFManager mc_PdfManager = new PDFManager();
        MainWinStates rsc = new MainWinStates();
        BABTestPaperMakerData mc_ProgramData = new BABTestPaperMakerData();

        const string STRLOADDATAPATH = "data.xml";
        string m_strSrcPath = "";
        bool bSrcSelROI = false;
        bool bDstSelROI = false;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = rsc;
            ivSrc.SetHalftone(true);
            ivDst.SetHalftone(true);
            ivSrc.delRoiSelected = del_SrcSelected;
            ivDst.delRoiSelected = del_TmpSelected;
            mc_PdfManager.delLoading = del_UpdateLoading;
            mc_PdfManager.delLoaded  = del_FinishLoad;
            ComponentDispatcher.ThreadIdle += fn_Init;   
        }

        private void fn_Init(object sender, EventArgs e)
        {
            ComponentDispatcher.ThreadIdle -= fn_Init;
            fn_LoadData();
            ivDst.SetFitScale();
        }

        private void fn_LoadData()
        {
            if (File.Exists(STRLOADDATAPATH))
            {
                XmlManager.LoadXml(STRLOADDATAPATH, mc_ProgramData);

                mc_PdfManager.dQuality = mc_ProgramData.PDFQulity;
                ivSrc.SetROIRect(0, new Rect(mc_ProgramData.SrcROI1X, mc_ProgramData.SrcROI1Y, mc_ProgramData.SrcROI1W, mc_ProgramData.SrcROI1H));
                ivSrc.SetROIRect(1, new Rect(mc_ProgramData.SrcROI2X, mc_ProgramData.SrcROI2Y, mc_ProgramData.SrcROI2W, mc_ProgramData.SrcROI2H));
                ivDst.SetROIRect(0, new Rect(mc_ProgramData.DstROI1X, mc_ProgramData.DstROI1Y, mc_ProgramData.DstROI1W, mc_ProgramData.DstROI1H));
                ivDst.SetROIRect(1, new Rect(mc_ProgramData.DstROI2X, mc_ProgramData.DstROI2Y, mc_ProgramData.DstROI2W, mc_ProgramData.DstROI2H));

                rsc.TemplatePath = mc_ProgramData.LastTemplatePath;

                ivDst.OpenImage(rsc.TemplatePath);
            }
            else
                mc_ProgramData.fn_Init();
        }

        private void fn_SaveData()
        {
            Rect rect = ivSrc.GetROIRect(0);
            mc_ProgramData.SrcROI1X = rect.X;
            mc_ProgramData.SrcROI1Y = rect.Y;
            mc_ProgramData.SrcROI1W = rect.Width;
            mc_ProgramData.SrcROI1H = rect.Height;
            rect = ivSrc.GetROIRect(1);
            mc_ProgramData.SrcROI2X = rect.X;
            mc_ProgramData.SrcROI2Y = rect.Y;
            mc_ProgramData.SrcROI2W = rect.Width;
            mc_ProgramData.SrcROI2H = rect.Height;

            rect = ivDst.GetROIRect(0);
            mc_ProgramData.DstROI1X = rect.X;
            mc_ProgramData.DstROI1Y = rect.Y;
            mc_ProgramData.DstROI1W = rect.Width;
            mc_ProgramData.DstROI1H = rect.Height;

            rect = ivDst.GetROIRect(1);
            mc_ProgramData.DstROI2X = rect.X;
            mc_ProgramData.DstROI2Y = rect.Y;
            mc_ProgramData.DstROI2W = rect.Width;
            mc_ProgramData.DstROI2H = rect.Height;

            mc_ProgramData.LastTemplatePath = rsc.TemplatePath;

            XmlManager.SaveXml(STRLOADDATAPATH, mc_ProgramData);
        }

        public bool del_SrcSelected()
        {
            bSrcSelROI = true;
            rsc.IsReady = bSrcSelROI && bDstSelROI;
            return true;
        }

        public bool del_TmpSelected()
        {
            bDstSelROI = true;
            rsc.IsReady = bSrcSelROI && bDstSelROI;
            return true;
        }

        public bool del_UpdateLoading(double dPercent, string strMsg = "")
        {
            bool bRtn = false;
            rsc.LoadingPercent = dPercent;
            rsc.LoadingMessage = strMsg;

            rsc.IsLoaded = false;
            rsc.ThisCursor = Cursors.Wait;
            return bRtn;
        }

        public bool del_FinishLoad()
        {
            bool bRtn = false;
            rsc.LoadingMessage = "Done!";
            rsc.IsLoaded = true;
            rsc.ThisCursor = Cursors.Arrow;
            rsc.SourcePath = m_strSrcPath;
            rsc.PageNum   = 1;
            rsc.PageTotal = mc_PdfManager.nPdfPageCount;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                fn_SetImage(rsc.PageNum - 1);
            }));
            return bRtn;
        }

        private void bn_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "PDF File (*.pdf)|*.pdf";
            if (dlg.ShowDialog() == true)
            {
                m_strSrcPath = dlg.FileName;
                mc_PdfManager.OpenPDF(m_strSrcPath);
            }
        }

        private void fn_SetImage(int page)
        {
            if (mc_PdfManager.m_listBitmap.Count <= page)
                page = mc_PdfManager.m_listBitmap.Count - 1;
            if (page < 0)
                page = 0;

            if (mc_PdfManager.m_listBitmap.Count > page)
            {
                ivSrc.SetImage(mc_PdfManager.m_listBitmap[page]);
                ivSrc.SetFitScale();
            }
        }

        private void bn_PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (mc_PdfManager.m_listBitmap.Count <= --rsc.PageNum)
                rsc.PageNum = mc_PdfManager.m_listBitmap.Count;
            if (rsc.PageNum < 1)
                rsc.PageNum = 1;
            fn_SetImage(rsc.PageNum - 1);
        }

        private void bn_NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (mc_PdfManager.m_listBitmap.Count <= ++rsc.PageNum)
                rsc.PageNum = mc_PdfManager.m_listBitmap.Count;
            if (rsc.PageNum < 1)
                rsc.PageNum = 1;
            fn_SetImage(rsc.PageNum - 1);
        }

        private void bn_Test_Click(object sender, RoutedEventArgs e)
        {
            //Rect rectROI1 = ivSrc.GetROIRect(0);
            CroppedBitmap bmp1 = ivSrc.GetROIImage(0);
            WriteableBitmap img1 = new WriteableBitmap(bmp1);
            WriteableBitmap imgtarget = ivDst.fn_GetImageStream();
            Rect rectROI1 = ivDst.GetROIRect(0);
            if (img1 != null)
            {
                CopyImageTo(img1, imgtarget, rectROI1);
                ivDst.SetImage(imgtarget);
                ivDst.fn_SaveImage("Test.png");
            }
            //PrintDialog pd = new PrintDialog();
            //if(pd.ShowDialog() == true)
            //{
            //}
        }

        public void CopyImageTo(WriteableBitmap sourceImage, WriteableBitmap target, Rect recttarget)
        {
            sourceImage = ResizeWritableBitmap(sourceImage, (int)recttarget.Width, (int)recttarget.Height);
            FalconWpf.ImageViewer iv = new FalconWpf.ImageViewer();
            iv.SetImage(sourceImage);
            iv.fn_SaveImage("Resize.png");
            if (sourceImage != null)
            {
                int sourceBytesPerPixel = (int)(sourceImage.Format.BitsPerPixel / 8.0);
                int sourceBytesPerLine = sourceImage.PixelWidth * sourceBytesPerPixel;


                byte[] sourcePixels = new byte[sourceBytesPerLine * sourceImage.PixelHeight];
                sourceImage.CopyPixels(sourcePixels, sourceBytesPerLine, 0);

                Int32Rect targetRect = new Int32Rect((int)recttarget.X, (int)recttarget.Y, (int)recttarget.Width, (int)recttarget.Height);
                target.WritePixels(targetRect, sourcePixels, sourceBytesPerLine, 0);
            }
        }
        public WriteableBitmap ResizeWritableBitmap(WriteableBitmap wBitmap, int reqWidth, int reqHeight)
        {
            int OriWidth = (int)wBitmap.PixelWidth;
            int OriHeight = (int)wBitmap.PixelHeight;
            double nXFactor = (double)reqWidth /OriWidth;
            double nYFactor = (double)reqHeight/OriHeight;
            var s = new ScaleTransform(nXFactor, nYFactor);

            var res = new TransformedBitmap(wBitmap, s);

            int stride = res.PixelWidth * (wBitmap.Format.BitsPerPixel / 8);

            // Create data array to hold source pixel data
            byte[] data = new byte[stride * res.PixelHeight];

            // Copy source image pixels to the data array
            res.CopyPixels(data, stride, 0);

            // Create WriteableBitmap to copy the pixel data to.      
            WriteableBitmap target = new WriteableBitmap(res.PixelWidth
                , res.PixelHeight, res.DpiX, res.DpiY
                , res.Format, null);

            // Write the pixel data to the WriteableBitmap.
            target.WritePixels(new Int32Rect(0, 0
                , res.PixelWidth, res.PixelHeight)
                , data, stride, 0);

            return target;
        }

        private void bn_OpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image File (*.bmp; *.png; *.jpg)|*.bmp; *.png; *.jpg";
            if (dlg.ShowDialog() == true)
            {
                rsc.TemplatePath = dlg.FileName;
                ivDst.OpenImage(dlg.FileName);
                ivDst.SetFitScale();
            }
        }

        private void bn_Create_Click(object sender, RoutedEventArgs e)
        {
            fn_CreateTestPage();
            MessageBox.Show("생성 완료.");
        }

        private void fn_CreateTestPage()
        {
            string strFile = "Test";
            for (int i = 0; i < rsc.PageTotal; i++)
            {
                string strFileName = $"{strFile}_{i}.png";
                fn_SetImage(i);
                CroppedBitmap bmp1 = ivSrc.GetROIImage(0);
                if (bmp1 != null)
                {
                    WriteableBitmap img = new WriteableBitmap(bmp1);
                    WriteableBitmap imgtarget = ivDst.fn_GetImageStream();
                    Rect rectROI = ivDst.GetROIRect(0);
                    if (img != null)
                    {
                        CopyImageTo(img, imgtarget, rectROI);
                        ivDst.SetImage(imgtarget);
                        ivDst.fn_SaveImage(strFileName);
                    }
                }

                CroppedBitmap bmp2 = ivSrc.GetROIImage(1);
                if (bmp2 != null)
                {
                    WriteableBitmap img = new WriteableBitmap(bmp2);
                    WriteableBitmap imgtarget = ivDst.fn_GetImageStream();
                    Rect rectROI = ivDst.GetROIRect(1);
                    if (img != null)
                    {
                        CopyImageTo(img, imgtarget, rectROI);
                        ivDst.SetImage(imgtarget);
                        ivDst.fn_SaveImage(strFileName);
                    }
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ivSrc.SetFitScale();
            ivDst.SetFitScale();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            fn_SaveData();
        }
    }
}
