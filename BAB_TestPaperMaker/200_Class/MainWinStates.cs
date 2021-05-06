using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BAB_TestPaperMaker
{
    class MainWinStates : IPropertyChanged
    {
        private int nPageNum = 0;
        public int PageNum
        {
            get { return nPageNum; }
            set
            {
                nPageNum = value;
                OnPropertyChanged("PageNum");
            }
        }

        private int nPageTotal = 0;
        public int PageTotal
        {
            get { return nPageTotal; }
            set
            {
                nPageTotal = value;
                OnPropertyChanged("PageTotal");
            }
        }

        private double dLoadingPercent = 0.0;
        public double LoadingPercent
        {
            get { return dLoadingPercent; }
            set
            {
                dLoadingPercent = value;
                OnPropertyChanged("LoadingPercent");
            }
        }

        private string strLoadingMessage = "";
        public string LoadingMessage
        {
            get { return strLoadingMessage; }
            set
            {
                strLoadingMessage = value;
                OnPropertyChanged("LoadingMessage");
            }
        }

        private bool bLoaded = true;
        public bool IsLoaded
        {
            get { return bLoaded; }
            set
            {
                bLoaded = value;
                OnPropertyChanged("IsLoaded");
            }
        }

        private Cursor crsThisCursor = Cursors.Arrow;
        public Cursor ThisCursor
        {
            get { return crsThisCursor; }
            set
            {
                crsThisCursor = value;
                OnPropertyChanged("ThisCursor");
            }
        }

        private string strSourcePath = "";
        public string SourcePath
        {
            get { return strSourcePath; }
            set
            {
                strSourcePath = value;
                OnPropertyChanged("SourcePath");
            }
        }

        private string strTemplatePath = "";
        public string TemplatePath
        {
            get { return strTemplatePath; }
            set
            {
                strTemplatePath = value;
                OnPropertyChanged("TemplatePath");
            }
        }

        private bool bIsReady = false;
        public bool IsReady
        {
            get { return bIsReady; }
            set
            {
                bIsReady = value;
                OnPropertyChanged("IsReady");
            }
        }
    }
}
