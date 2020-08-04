using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace WMS.Reports
{
    public partial class PurchaseOrderPrint : DevExpress.XtraReports.UI.XtraReport
    {
        public PurchaseOrderPrint()
        {
            InitializeComponent();
        }

        private void PurchaseOrderPrint_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {

        }
    }
}
