using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FalconWpf
{
    public delegate void delWriteLog(string msg);
    public delegate void ReturnAngle(double angle);
    public delegate void ReturnMilling(int index, Rect rect);
    public delegate void delUpdateInfo(int x, int y, int a, int r, int g, int b);
    public delegate void delContextMenu(ImageViewer.EnRoiMode mode, ImageViewer.EnObjectSelect objsel);
    /// <summary>
    /// Align.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        public delWriteLog delwritelog = null;
        public ReturnAngle delAngle = null;
        public ReturnMilling delMilling = null;
        public delUpdateInfo delInfo = null;
        public delContextMenu delCM = null;
        string Title = "ImageViewer";
        Color clrLine = Colors.White;
        Color clrFill = Colors.White;
        double dThickness = 0;

        public Func<bool> delRoiSelected = null;

        #region define enum
        //---------------------------------------------------------------------------
        /**
        @enum   EnRoiMode	
        @brief	UserControl의 Draw 모드 구분 열거형.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:32
        */
        public enum EnRoiMode
        {
            ModeClear = -1,
            ModeMove = 0,
            ModeSelect
        }

        //---------------------------------------------------------------------------
        /**
        @enum   EnObjectSelect	
        @brief	UserControl의 선택 모드 구분 열거형.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:32
        */
        public enum EnObjectSelect
        {
            Select1 = 100,
            Select2
        }

        #endregion

        #region define Member Variable
        double m_dScale = 1.0;
        int m_nChildCount = 0;
        Point m_pntPrev;

        public EnRoiMode m_enMode = 0;
        public EnObjectSelect m_enSelObj = EnObjectSelect.Select1;

        ImageBrush m_ImgBrsOrg;
        ImageBrush m_ImgBrs;

        Rect[] m_rectROI = new Rect[Enum.GetNames(typeof(EnObjectSelect)).Length];

        Rectangle rectSelect1 = new Rectangle();
        Rectangle rectSelect2 = new Rectangle();


        private double m_Theta = 360;
        public double dTheta { get { return m_Theta; } }

        private int m_nWidth;
        private int m_nHeight;
        private int m_nChannel;
        private int m_nStride;
        private string m_strFileName = string.Empty;
        private string m_strPath = string.Empty;
        private string m_strExtension = string.Empty;
        private string m_strCreationTime = string.Empty;
        private double m_dDpiX;
        private double m_dDpiY;
        public string FileName { get { return m_strFileName; } set { m_strFileName = value; } }
        public int nWidth { get { return m_nWidth; } }
        public int nHeight { get { return m_nHeight; } }
        public int Channel { get { return m_nChannel; } }
        public string Path { get { return m_strPath; } }
        public string Extension { get { return m_strExtension; } }
        public string CreationTime { get { return m_strCreationTime; } }
        public int Stride { get { return m_nStride; } }
        public double DpiX { get { return m_dDpiX; } }
        public double DpiY { get { return m_dDpiY; } }
        #endregion

        public ImageViewer()
        {
            InitializeComponent();
            ComponentDispatcher.ThreadIdle += fn_InitUI;
        }

        /**	
        @fn		public void fn_InitUI(object sender, EventArgs e)
        @brief	UI Load된 후 Class 초기화.
        @return	void
        @param	object    sender :
        @param	EventArgs e      :
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:56
        */
        public void fn_InitUI(object sender, EventArgs e)
        {
            ComponentDispatcher.ThreadIdle -= fn_InitUI;

            // Select1
            rectSelect1.Fill = Brushes.Transparent;
            rectSelect1.Width = 0;
            rectSelect1.Height = 0;
            rectSelect1.Stroke = Brushes.Magenta;
            rectSelect1.StrokeDashArray.Add(2);
            rectSelect1.StrokeDashArray.Add(2);

            lib_Canvas.Children.Add(rectSelect1);

            Canvas.SetLeft(rectSelect1, -1);
            Canvas.SetTop(rectSelect1, -1);

            // Select2
            rectSelect2.Fill = Brushes.Transparent;
            rectSelect2.Width = 0;
            rectSelect2.Height = 0;
            rectSelect2.Stroke = Brushes.Magenta;
            rectSelect2.StrokeDashArray.Add(2);
            rectSelect2.StrokeDashArray.Add(2);

            lib_Canvas.Children.Add(rectSelect2);

            Canvas.SetLeft(rectSelect2, -1);
            Canvas.SetTop(rectSelect2, -1);

            m_nChildCount = lib_Canvas.Children.Count;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void OpenImage(string strPath)
        @brief	Align Control에 string path로 이미지 Open.
        @return	
        @param	
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:39
        */
        public void OpenImage(string strPath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                try
                {
                    if (!File.Exists(strPath))
                    {
                        m_ImgBrs = null;
                        m_ImgBrsOrg = null;
                        lib_Canvas.Background = Brushes.Transparent;
                        return;
                    }
                    FileInfo fi = new FileInfo(strPath);
                    m_strPath = strPath;
                    m_strFileName = strPath.Substring(strPath.LastIndexOf('\\') + 1);
                    m_strExtension = fi.Extension;
                    m_strCreationTime = fi.CreationTime.ToString("yyyy.MM.dd HH:mm:ss");

                    BitmapImage bmpImg = new BitmapImage();
                    FileStream source = File.OpenRead(strPath);
                    //bmpImg.Format = PixelFormats.Bgr32;
                    bmpImg.BeginInit();
                    bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImg.StreamSource = source;
                    bmpImg.EndInit();

                    WriteableBitmap wbm = null;
                    FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

                    // BitmapSource objects like FormatConvertedBitmap can only have their properties
                    // changed within a BeginInit/EndInit block.
                    newFormatedBitmapSource.BeginInit();

                    // Use the BitmapSource object defined above as the source for this new
                    // BitmapSource (chain the BitmapSource objects together).
                    newFormatedBitmapSource.Source = bmpImg;

                    // Set the new format to Gray32Float (grayscale).
                    newFormatedBitmapSource.DestinationFormat = PixelFormats.Bgr24;
                    newFormatedBitmapSource.EndInit();

                    wbm = new WriteableBitmap(newFormatedBitmapSource);

                    m_ImgBrs = new ImageBrush(wbm);
                    m_ImgBrs.Stretch = Stretch.None;
                    lib_Canvas.Width = m_ImgBrs.ImageSource.Width;
                    lib_Canvas.Height = m_ImgBrs.ImageSource.Height;
                    lib_Canvas.Background = m_ImgBrs;
                    m_nWidth = wbm.PixelWidth;
                    m_nHeight = wbm.PixelHeight;
                    m_nChannel = (int)(wbm.Format.BitsPerPixel / 8.0);
                    m_nStride = (m_nWidth * m_nChannel + 3) & ~3;
                    m_dDpiX = wbm.DpiX;
                    m_dDpiY = wbm.DpiY;

                    m_ImgBrsOrg = m_ImgBrs.Clone();

                    m_dScale = myScaleTransform.ScaleX;
                }
                catch(Exception)
                {

                }
            }));
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public double GetFitScale()
        @brief	Control의 화면에 이미지 크기 맞게 배율 계산.
        @return	
        @param	
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:39
        */
        public double GetFitScale()
        {
            double dScaleX = 1.0;
            double dScaleY = 1.0;
            if (m_ImgBrs != null)
            {
                try
                {
                    if (ctrl_Grid.ActualWidth > 0 && ctrl_Grid.ActualHeight > 0)
                    {
                        dScaleX = ctrl_Grid.ActualWidth / m_ImgBrs.ImageSource.Width;
                        dScaleY = ctrl_Grid.ActualHeight / m_ImgBrs.ImageSource.Height;
                    }
                }
                catch (System.Exception ex)
                {
                    delwritelog?.Invoke($"{this.Title} : {ex.Message}");
                }
            }
            return (dScaleY > dScaleX) ? dScaleX : dScaleY;
        }

        /**	
        @fn		public void SetFitScale()
        @brief	Fit Scale.
        @return	void
        @param	void
        @remark	
         - Canvas Fit Scale.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:58
        */
        public void SetFitScale()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                sl_Scale.Value = GetFitScale();
            }));
        }

        /**	
        @fn		public void SetHalftone(bool bHalftone = false)
        @brief	Canvas Halftone Setting.
        @return	void
        @param	bool bHalftone : Halftone 여부.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:59
        */
        public void SetHalftone(bool bHalftone = false)
        {
            if (bHalftone)
                RenderOptions.SetBitmapScalingMode(lib_Canvas, BitmapScalingMode.HighQuality);
            else
                RenderOptions.SetBitmapScalingMode(lib_Canvas, BitmapScalingMode.NearestNeighbor);
        }

        /**	
        @fn		public bool GetHalftone()
        @brief	Halftone 여부 얻기.
        @return	bool : Halftone 여부.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:00
        */
        public bool GetHalftone()
        {
            bool bRtn = false;
            if (RenderOptions.GetBitmapScalingMode(lib_Canvas) != BitmapScalingMode.NearestNeighbor)
                bRtn = true;
            else
                bRtn = false;
            return bRtn;
        }

        /**	
        @fn		public void SetScale(double dScale)
        @brief	원하는 Scale값으로 설정.
        @return	void
        @param	double dScale : 원하는 Scale.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:02
        */
        public void SetScale(double dScale)
        {
            sl_Scale.Value = dScale;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		internal void SetImage(WriteableBitmap wb, Stretch stretch = Stretch.None)
        @brief	WriteableBitmap Type을 Align Control에 Set.
        @return	void
        @param	WriteableBitmap wb      : Source Image.
        @param	Stretch         stretch : Stretch Option.
        @remark	
         - Stretch Option을 Fill로 할 경우, 배율이 맞지 않을 수 있음.
         - 배율 조정 + Fit을 원하면 GetFitScale을 사용하여 수동으로 화면 Scale 조정 할 것.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(WriteableBitmap wb, Stretch stretch = Stretch.None, bool bUpdateOrg = true)
        {
            if (wb == null)
                return;

            m_ImgBrs = new ImageBrush(wb);

            m_ImgBrs.Stretch = stretch;
            if (m_ImgBrs != null && bUpdateOrg)
                m_ImgBrsOrg = m_ImgBrs.Clone();

            m_nWidth = wb.PixelWidth;
            m_nHeight = wb.PixelHeight;
            m_nChannel = (int)(wb.Format.BitsPerPixel / 8.0);
            if (m_ImgBrs.Stretch == Stretch.None)
            {
                lib_Canvas.Width = m_nWidth;
                lib_Canvas.Height = m_nHeight;
            }
            lib_Canvas.Background = m_ImgBrs;
            m_dScale = myScaleTransform.ScaleX;
        }

        /**	
        @fn		public void SetImage(byte[] buff, int width, int height)
        @brief	byte array를 이미지 설정.
        @return	void
        @param	byte[]  buff    : Image Buffer
        @param	int     width   : Image Width
        @param	int     height  : Image Height
        @remark	
         - GrayScale Image Set.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:04
        */
        public void SetImage(byte[] buff, int width, int height)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate ()
            {
                if (this.IsLoaded)
                {
                    WriteableBitmap wbm = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
                    wbm.Lock();
                    wbm.WritePixels(new Int32Rect(0, 0, width, height), buff, width, 0);
                    wbm.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    wbm.Unlock();
                    SetImage(wbm);
                    SetFitScale();
                }
            }));
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void SetImage(BitmapImage bmpimg, Stretch stretch = Stretch.None)
        @brief	WriteableBitmap Type을 Align Control에 Set.
        @return	void
        @param	BitmapImage     bmpimg  : Source Image.
        @param	Stretch         stretch : Stretch Option.
        @remark	
         - Stretch Option을 Fill로 할 경우, 배율이 맞지 않을 수 있음.
         - 배율 조정 + Fit을 원하면 GetFitScale을 사용하여 수동으로 화면 Scale 조정 할 것.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(BitmapImage bmpimg, Stretch stretch = Stretch.None)
        {
            WriteableBitmap wbm = null;
            
            FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

            // BitmapSource objects like FormatConvertedBitmap can only have their properties
            // changed within a BeginInit/EndInit block.
            newFormatedBitmapSource.BeginInit();

            // Use the BitmapSource object defined above as the source for this new
            // BitmapSource (chain the BitmapSource objects together).
            newFormatedBitmapSource.Source = bmpimg;

            // Set the new format to Gray32Float (grayscale).
            newFormatedBitmapSource.DestinationFormat = PixelFormats.Bgr24;
            newFormatedBitmapSource.EndInit();

            wbm = new WriteableBitmap(newFormatedBitmapSource);
            
            m_ImgBrs = new ImageBrush(wbm);
            m_ImgBrs.Stretch = stretch;
            if (m_ImgBrs != null)
                m_ImgBrsOrg = m_ImgBrs.Clone();
            if (m_ImgBrs.Stretch == Stretch.None)
            {
                lib_Canvas.Width = m_ImgBrs.ImageSource.Width;
                lib_Canvas.Height = m_ImgBrs.ImageSource.Height;
            }

            m_nWidth = (int)m_ImgBrs.ImageSource.Width;
            m_nHeight = (int)m_ImgBrs.ImageSource.Height;
            m_nChannel = (int)(wbm.Format.BitsPerPixel / 8.0);

            lib_Canvas.Background = new ImageBrush(wbm);
            m_dScale = myScaleTransform.ScaleX;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void SetImage(IntPtr ptr, double dWidth, double dHeight, double dChannel)
        @brief	IntPtr 이미지를 Contorl에 Set.
        @return	void
        @param	IntPtr ptr          : Source Image Integer Pointer
        @param	double dWidth       : Image Width
        @param	double dHeight      : Image Height
        @param	double dChannel     : Image Channel
        @remark	
         - Image Pointer를 Byte Array로 변환.
         - 변환된 ByteArray를 Writeable Bitmap으로 변환.
         - Writeable Bitmap을 Control에 Set.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(IntPtr ptr, double dWidth, double dHeight, double dChannel)
        {
            int nImageLength = (int)(dWidth * dHeight * dChannel);
            byte[] imageData = new byte[nImageLength];
            Marshal.Copy(ptr, imageData, 0, nImageLength);
            WriteableBitmap wbm = new WriteableBitmap((int)dWidth, (int)dHeight, 96, 96, PixelFormats.Gray8, null);
            wbm.Lock();
            wbm.WritePixels(new Int32Rect(0, 0, (int)dWidth, (int)dHeight), imageData, (int)dWidth, 0);
            wbm.AddDirtyRect(new Int32Rect(0, 0, (int)dWidth, (int)dHeight));
            wbm.Unlock();
            SetImage(wbm);
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private void onMouseDown(object sender, MouseButtonEventArgs e)
        @brief	마우스 Down 이벤트.
        @return	void
        @param	object                  sender
        @param	MouseButtonEventArgs    e
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:44
        */
        private void onMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_pntPrev = e.GetPosition(lib_Canvas);
                if (m_enSelObj == EnObjectSelect.Select1)
                {
                    rectSelect1.Width = 0;
                    rectSelect1.Height = 0;
                    Canvas.SetLeft(rectSelect1, m_pntPrev.X);
                    Canvas.SetTop(rectSelect1, m_pntPrev.Y);
                }
                else if (m_enSelObj == EnObjectSelect.Select2)
                {
                    rectSelect2.Width = 0;
                    rectSelect2.Height = 0;
                    Canvas.SetLeft(rectSelect2, m_pntPrev.X);
                    Canvas.SetTop(rectSelect2, m_pntPrev.Y);
                }
                delRoiSelected?.Invoke();
            }
            else if(e.RightButton == MouseButtonState.Pressed)
            {
                m_pntPrev = e.GetPosition(this);
                WriteableBitmap bitmapImage = m_ImgBrs.ImageSource as WriteableBitmap;
                if (bitmapImage != null)
                {
                    int height = bitmapImage.PixelHeight;
                    int width = bitmapImage.PixelWidth;
                    int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
                    byte[] pixelByteArray = new byte[4];
                    try
                    {
                        bitmapImage.CopyPixels(new Int32Rect((int)e.GetPosition(lib_Canvas).X, (int)e.GetPosition(lib_Canvas).Y, 1, 1), pixelByteArray, nStride, 0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    switch (bitmapImage.Format.BitsPerPixel)
                    {
                        case 8:
                            pixelByteArray[1] = pixelByteArray[0];
                            pixelByteArray[2] = pixelByteArray[0];
                            pixelByteArray[3] = 255;
                            break;
                        case 24:
                            pixelByteArray[3] = 255;
                            break;
                        case 32:
                            break;
                    }
                    delInfo?.Invoke((int)e.GetPosition(lib_Canvas).X, (int)e.GetPosition(lib_Canvas).Y, pixelByteArray[3], pixelByteArray[2], pixelByteArray[1], pixelByteArray[0]);
                }
            }
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private void onMouseMove(object sender, MouseEventArgs e)
        @brief	마우스 Move 이벤트.
        @return	void
        @param	object          sender
        @param	MouseEventArgs  e
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:46
        */
        private void onMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                Point pntCurr = e.GetPosition(lib_Canvas);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    double dGapX = pntCurr.X - m_pntPrev.X;
                    double dGapY = pntCurr.Y - m_pntPrev.Y;

                    if (m_enSelObj == EnObjectSelect.Select1)
                    {
                        if (dGapX < 0)
                            Canvas.SetLeft(rectSelect1, pntCurr.X);
                        rectSelect1.Width = Math.Abs(dGapX);

                        if (dGapY < 0)
                            Canvas.SetTop(rectSelect1, pntCurr.Y);
                        rectSelect1.Height = Math.Abs(dGapY);
                    }
                    if (m_enSelObj == EnObjectSelect.Select2)
                    {
                        if (dGapX < 0)
                            Canvas.SetLeft(rectSelect2, pntCurr.X);
                        rectSelect2.Width = Math.Abs(dGapX);

                        if (dGapY < 0)
                            Canvas.SetTop(rectSelect2, pntCurr.Y);
                        rectSelect2.Height = Math.Abs(dGapY);
                    }
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    double dOffsetX = lib_ScrollViewer.HorizontalOffset;
                    double dOffsetY = lib_ScrollViewer.VerticalOffset;
                    Point pnt = new Point();
                    pnt.X = m_pntPrev.X - e.GetPosition(this).X;
                    pnt.Y = m_pntPrev.Y - e.GetPosition(this).Y;
                    lib_ScrollViewer.ScrollToHorizontalOffset(dOffsetX + pnt.X);
                    lib_ScrollViewer.ScrollToVerticalOffset(dOffsetY + pnt.Y);
                    m_pntPrev = e.GetPosition(this);
                }
            }
            catch (Exception ex)
            {
                delwritelog?.Invoke($"{this.Title} : {ex.Message}");
            }
        }

        /**	
        @fn		private void onMouseUp(object sender, MouseButtonEventArgs e)
        @brief	Mouse Up (Preview Mouse Up Event)
        @return	void
        @param	object               sender :
        @param	MouseButtonEventArgs e      :
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:11
        */
        private void onMouseUp(object sender, MouseButtonEventArgs e)
        {
            
            try
            {
                if (m_enSelObj == EnObjectSelect.Select1)
                {
                    m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1].X = Canvas.GetLeft(rectSelect1);
                    m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1].Y = Canvas.GetTop(rectSelect1);
                    m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1].Width = rectSelect1.Width;
                    m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1].Height = rectSelect1.Height;
                }
                else if (m_enSelObj == EnObjectSelect.Select2)
                {
                    m_rectROI[(int)EnObjectSelect.Select2 - (int)EnObjectSelect.Select1].X = Canvas.GetLeft(rectSelect2);
                    m_rectROI[(int)EnObjectSelect.Select2 - (int)EnObjectSelect.Select1].Y = Canvas.GetTop(rectSelect2);
                    m_rectROI[(int)EnObjectSelect.Select2 - (int)EnObjectSelect.Select1].Width = rectSelect2.Width;
                    m_rectROI[(int)EnObjectSelect.Select2 - (int)EnObjectSelect.Select1].Height = rectSelect2.Height;
                }
            }
            catch (Exception ex)
            {
                delwritelog?.Invoke($"{this.Title} : {ex.Message}");
            }
        }

        /**	
        @fn		public void SetMode(EnRoiMode mode, EnDrawMode drawmode = 0)
        @brief	Draw Mode 설정.
        @return	void
        @param	EnRoiMode  mode     : ROI Mode
        @param	EnDrawMode drawmode : Draw Mode
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:13
        */
        public void SetMode(EnRoiMode mode)
        {
            // Clear
            if (mode == EnRoiMode.ModeClear)
            {
                int childrencnt = lib_Canvas.Children.Count;
                if (childrencnt > 0)
                {
                    for (int i = childrencnt - 1; i > m_nChildCount - 1; i--)
                    {
                        lib_Canvas.Children.RemoveAt(i);
                    }
                }
            }
            else
            {
                m_enMode = mode;
                
                if (m_enMode == EnRoiMode.ModeMove)
                {
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    this.Cursor = Cursors.Cross;
                }
                //fn_SetContextMenu(m_enMode, m_enSelObj);
                delCM?.Invoke(m_enMode, m_enSelObj);
            }
        }

        /**	
        @fn		public void SetThickness(int thickness)
        @brief	객체 Thickness 설정.
        @return	void
        @param	int thickness : 목표 두께.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:15
        */
        public void SetThickness(int thickness)
        {
            dThickness = thickness;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private bool CheckRectInsidePoint(Rect rect, Point pnt)
        @brief	인자로 넘어온 사각형 안에 포인트가 속해 있는지 bool형으로 리턴.
        @return	bool : Point가 Rect 안에 있는지.
        @param	Rect rect : 확인할 영역.
        @param	Point pnt : 확인할 점.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:49
        */
        private bool CheckRectInsidePoint(Rect rect, Point pnt)
        {
            return (rect.Left <= pnt.X) && (rect.Top <= pnt.Y) && (rect.Right >= pnt.X) && (rect.Bottom >= pnt.Y);
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void ZoomScale(double dScale)
        @brief	Zoom Scale 설정.
        @return	void
        @param	double dScale : 실수형 줌 값.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:51
        */
        private void ZoomScale(double dScale)
        {
            if (myScaleTransform != null && m_ImgBrs != null)
            {
                double dCurPosRateX = 0.0;
                double dCurPosRateY = 0.0;
                double width = m_ImgBrs.ImageSource.Width;
                double height = m_ImgBrs.ImageSource.Height;
                if (lib_ScrollViewer.ExtentWidth > 0)
                    dCurPosRateX = (lib_ScrollViewer.HorizontalOffset + lib_ScrollViewer.ActualWidth / 2) / lib_ScrollViewer.ExtentWidth;
                if (lib_ScrollViewer.ExtentHeight > 0)
                    dCurPosRateY = (lib_ScrollViewer.VerticalOffset + lib_ScrollViewer.ActualHeight / 2) / lib_ScrollViewer.ExtentHeight;

                myScaleTransform.ScaleX = dScale;
                myScaleTransform.ScaleY = dScale;
                m_dScale = dScale;

                double dOffsetX = (width * dScale) * dCurPosRateX - lib_ScrollViewer.ActualWidth / 2;
                double dOffsetY = (height * dScale) * dCurPosRateY - lib_ScrollViewer.ActualHeight / 2;
                lib_ScrollViewer.ScrollToHorizontalOffset(dOffsetX);
                lib_ScrollViewer.ScrollToVerticalOffset(dOffsetY);
            }
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public double GetZoomScale()
        @brief	Control에 설정된 Zoom Scale 반환.
        @return	double : 현재 Zoom Scale.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:52
        */
        public double GetZoomScale()
        {
            return m_dScale;
        }


        //---------------------------------------------------------------------------
        /**	
        @fn		public CroppedBitmap GetModelImage()
        @brief	Model ROI에서 이미지 얻기.
        @return	CroppedBitmap : Model ROI Image
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  17:03
        */
        public CroppedBitmap GetROIImage(int offset)
        {
            Rect rectROI = m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1 + offset];
            CroppedBitmap cb = null;
            if (rectROI.Width > 0 && rectROI.Height > 0 && lib_Canvas.Background != null)
            {
                ImageBrush ib = lib_Canvas.Background as ImageBrush;
                if (ib != null)
                {
                    WriteableBitmap wb = ib.ImageSource as WriteableBitmap;
                    if (wb != null)
                    {
                        cb = new CroppedBitmap(
                            wb,
                            new Int32Rect((int)rectROI.X, (int)rectROI.Y,
                            (int)rectROI.Width, (int)rectROI.Height));       //select region rect
                    }
                }
            }
            
            return cb;
        }

        /**	
        @fn		public Rect GetROIRect()
        @brief	Select ROI 얻기.
        @return	Rect : ROI 객체.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:16
        */
        public Rect GetROIRect(int offset)
        {
            return m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1 + offset];
        }

        public void SetROIRect(int offset, Rect rectangle)
        {
            m_rectROI[(int)EnObjectSelect.Select1 - (int)EnObjectSelect.Select1 + offset] = rectangle;
            switch (offset)
            {
                case 0:
                    rectSelect1.Width  = rectangle.Width;
                    rectSelect1.Height = rectangle.Height;
                    Canvas.SetLeft(rectSelect1, rectangle.X);
                    Canvas.SetTop (rectSelect1, rectangle.Y);
                    break;
                case 1:
                    rectSelect2.Width = rectangle.Width;
                    rectSelect2.Height = rectangle.Height;
                    Canvas.SetLeft(rectSelect2, rectangle.X);
                    Canvas.SetTop (rectSelect2, rectangle.Y);
                    break;
            }
            //rectSelect2
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public IntPtr? fn_GetIamgePtr()
        @brief	Image IntPtr 얻기.
        @return	IntPtr? : nullable Image Pointer
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:32
        */
        public IntPtr? fn_GetImagePtr()
        {
            if (m_ImgBrs != null)
            {
                try
                {
                    WriteableBitmap bmp = m_ImgBrs.ImageSource as WriteableBitmap;
                    return bmp.BackBuffer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }

        /**	
        @fn		public IntPtr? fn_GetImgOrgPtr()
        @brief	원본 이미지 포인터 얻기.
        @return	IntPtr? : 원본 이미지 포인터.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/4/7  16:21
        */
        public IntPtr? fn_GetImgOrgPtr()
        {
            if (m_ImgBrsOrg != null)
            {
                try
                {
                    WriteableBitmap bmp = m_ImgBrsOrg.ImageSource as WriteableBitmap;
                    return bmp.BackBuffer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }

        /**	
        @fn		public void fn_OriginalImage()
        @brief	이미지 원본으로.
        @return	void
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/4/7  16:20
        */
        public void fn_OriginalImage(bool bFlag = false)
        {
            if (m_ImgBrsOrg != null)
            {
                if (!bFlag)
                {
                    lib_Canvas.Width  = m_ImgBrsOrg.ImageSource.Width;
                    lib_Canvas.Height = m_ImgBrsOrg.ImageSource.Height;
                    m_nWidth  = (int)m_ImgBrsOrg.ImageSource.Width;
                    m_nHeight = (int)m_ImgBrsOrg.ImageSource.Height;
                    //m_nChannel = m_ImgBrsOrg.ImageSource.;
                }
                lib_Canvas.Background = m_ImgBrsOrg;
                m_dScale = myScaleTransform.ScaleX;
            }
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public WriteableBitmap fn_GetImageStream()
        @brief	Image Steam 얻기.
        @return	WriteableBitmap : Control의 이미지.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:33
        */
        public WriteableBitmap fn_GetImageStream()
        {
            if (m_ImgBrs != null)
            {
                try
                {
                    WriteableBitmap bmp = null;
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                        bmp = m_ImgBrs.ImageSource as WriteableBitmap;
                    }));
                    return bmp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void fn_SaveImage(string strPath)
        @brief	Image Save.
        @return	void
        @param	string strPath : Image Path.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:34
        */
        public void fn_SaveImage(string strPath, bool bOrigin = false, bool bRender = false)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (m_ImgBrs != null)
                {
                    try
                    {
                        WriteableBitmap bmp;
                        if (bOrigin)
                            bmp = m_ImgBrsOrg.ImageSource as WriteableBitmap;
                        else
                            bmp = m_ImgBrs.ImageSource as WriteableBitmap;

                        if (bRender)
                        {
                            Transform transform = lib_Canvas.LayoutTransform;

                            lib_Canvas.LayoutTransform = null;
                            Size size = new Size(lib_Canvas.Width, lib_Canvas.Height);

                            Size back = lib_Canvas.DesiredSize;

                            lib_Canvas.Measure(size);
                            lib_Canvas.Arrange(new Rect(size));
                            RenderTargetBitmap rb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Default);
                            rb.Render(lib_Canvas);
                            FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
                            newFormatedBitmapSource.BeginInit();
                            newFormatedBitmapSource.Source = rb;
                            newFormatedBitmapSource.DestinationFormat = bmp.Format;
                            newFormatedBitmapSource.EndInit();

                            using (FileStream stream = new FileStream(strPath, FileMode.Create))
                            {
                                //PngBitmapEncoder encoder = new PngBitmapEncoder();
                                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                                //encoder.Frames.Add(BitmapFrame.Create(bmp));
                                encoder.Frames.Add(BitmapFrame.Create(newFormatedBitmapSource));
                                encoder.Save(stream);
                            }
                            lib_Canvas.Measure(back);
                            lib_Canvas.LayoutTransform = transform;
                        }
                        else
                        {
                            using (FileStream stream = new FileStream(strPath, FileMode.Create))
                            {
                                //PngBitmapEncoder encoder = new PngBitmapEncoder();
                                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(bmp));
                                encoder.Save(stream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        delwritelog?.Invoke($"{this.Title} : {ex.Message}");
                    }
                }
            }));
        }

        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double dZoom = GetZoomScale();
            if (e.Delta > 0)
                sl_Scale.Value = dZoom + 0.1;
            else
                sl_Scale.Value = dZoom - 0.1;
        }

        public void ClearCanvas()
        {
            if (lib_Canvas != null)
            {
                lib_Canvas.Background = Brushes.Transparent;
                m_ImgBrs = null;
                m_ImgBrsOrg = null;
            }
        }

        private void sl_Scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                ZoomScale(slider.Value);
                lb_Scale.Header = slider.Value.ToString("0.0%");
                rectSelect1.StrokeThickness = 1 / m_dScale;
                rectSelect2.StrokeThickness = 1 / m_dScale;
            }
        }

        private void bn_FitScale_Click(object sender, RoutedEventArgs e)
        {
            SetFitScale();
        }

        private void bn_Scale1_Click(object sender, RoutedEventArgs e)
        {
            sl_Scale.Value = 1.0;
        }

        public void SetLineColor(Color clr)
        {
            clrLine = clr;
        }

        public void SetFillColor(Color clr)
        {
            clrFill = clr;
        }

        public Color GetLineColor()
        {
            return clrLine;
        }

        public Color GetFillColor()
        {
            return clrFill;
        }

        public void SelectObject(EnObjectSelect enObj)
        {
            m_enSelObj = enObj;
        }

        public void RenderImage(bool bOrigin = false)
        {
            if (m_ImgBrs != null)
            {
                try
                {
                    WriteableBitmap bmp;
                    if (bOrigin)
                        bmp = m_ImgBrsOrg.ImageSource as WriteableBitmap;
                    else
                        bmp = m_ImgBrs.ImageSource as WriteableBitmap;

                    Transform transform = lib_Canvas.LayoutTransform;
                    lib_Canvas.LayoutTransform = null;

                    Size size = new Size(lib_Canvas.Width, lib_Canvas.Height);
                    Size back = lib_Canvas.DesiredSize;

                    lib_Canvas.Measure(size);
                    lib_Canvas.Arrange(new Rect(size));
                    RenderTargetBitmap rb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Default);
                    rb.Render(lib_Canvas);
                    
                    //PngBitmapEncoder encoder = new PngBitmapEncoder();
                    BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                    //encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Frames.Add(BitmapFrame.Create(rb));

                    m_ImgBrs.ImageSource = new WriteableBitmap(encoder.Frames[0]);
                    lib_Canvas.Measure(back);
                    lib_Canvas.LayoutTransform = transform;
                    lib_Canvas.Background = m_ImgBrs;
                }
                catch (Exception ex)
                {
                    delwritelog?.Invoke($"{this.Title} : {ex.Message}");
                }
            }
        }

        private void bn_Halftone_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                SetHalftone(mi.IsChecked);
            }
        }

        private void bn_RenderImage_Click(object sender, RoutedEventArgs e)
        {
            RenderImage();
        }

        private void cm_Move_Click(object sender, RoutedEventArgs e)
        {
            SelectObject(EnObjectSelect.Select1);
            SetMode(EnRoiMode.ModeMove);
        }

        private void cm_Select1_Click(object sender, RoutedEventArgs e)
        {
            SelectObject(EnObjectSelect.Select1);
            SetMode(EnRoiMode.ModeSelect);
        }

        private void cm_Select2_Click(object sender, RoutedEventArgs e)
        {
            SelectObject(EnObjectSelect.Select2);
            SetMode(EnRoiMode.ModeSelect);
        }


        private void fn_SetContextMenu(EnRoiMode mode, EnObjectSelect obj)
        {
//             try
//             {
//                 for (int i = 0; i < cm_Right.Items.Count; i++)
//                 {
//                     (cm_Right.Items[i] as MenuItem).IsChecked = false;
//                 }
//                 switch (mode)
//                 {
//                     case EnRoiMode.ModeMove:
//                         (cm_Right.Items[0] as MenuItem).IsChecked = true;
//                         break;
//                     case EnRoiMode.ModeSelect:
//                         switch (obj)
//                         {
//                             case EnObjectSelect.Select1:
//                                 (cm_Right.Items[1] as MenuItem).IsChecked = true;
//                                 break;
//                             case EnObjectSelect.Select2:
//                                 (cm_Right.Items[5] as MenuItem).IsChecked = true;
//                                 break;
//                         }
//                         break;
//                 }
//             }
//             catch { }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb != null)
            {
                switch(cb.SelectedIndex)
                {
                    case 0:
                        m_enSelObj = EnObjectSelect.Select1;
                        break;
                    case 1:
                        m_enSelObj = EnObjectSelect.Select2;
                        break;
                }
            }
        }
    }
}
