using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BAB_TestPaperMaker
{
    class BABTestPaperMakerData
    {
        public double PDFQulity { get; set; }
        public double SrcROI1X{ get; set; }
        public double SrcROI1Y{ get; set; }
        public double SrcROI1W{ get; set; }
        public double SrcROI1H{ get; set; }
        public double SrcROI2X { get; set; }
        public double SrcROI2Y { get; set; }
        public double SrcROI2W { get; set; }
        public double SrcROI2H { get; set; }
        public double DstROI1X { get; set; }
        public double DstROI1Y { get; set; }
        public double DstROI1W { get; set; }
        public double DstROI1H { get; set; }
        public double DstROI2X { get; set; }
        public double DstROI2Y { get; set; }
        public double DstROI2W { get; set; }
        public double DstROI2H { get; set; }

        public string LastTemplatePath { get; set; }

        public void fn_Init()
        {
            PDFQulity = 300;
            LastTemplatePath = string.Empty;
        }
    }
}
