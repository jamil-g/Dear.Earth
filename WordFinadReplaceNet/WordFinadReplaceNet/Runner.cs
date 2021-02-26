using System;
using log4net;
using Microsoft.Office.Interop.Word;

namespace WordFinadReplaceNet
{
    class Runner
    {
        #region properties and members definition
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region public methods
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Find & Replace Sample");
                GenerateReport generateReport = new GenerateReport();
                GenerateReport.Coordinates coordinates = new GenerateReport.Coordinates { lon = 32.80f, lat = 34.977f, zoomlevel = 15 };
                GenerateReport.Marks marks = new GenerateReport.Marks { AirQuality = 5.2f, Ecology = 7f, NoiseInterference = 8.9f, RadiationEG = 6.7f, SoilPollution = 5.8f, FinalMark = 6.0f };
                GenerateReport.ReportExtraInfo extrainfo = new GenerateReport.ReportExtraInfo();
                extrainfo.ProjName = "New Project"; extrainfo.CustomerName = "New Customer";
                generateReport.GenerateReportPdf(marks, coordinates, extrainfo);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
        #endregion
    }
}
