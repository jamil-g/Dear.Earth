using System;
using System.Drawing;
using System.IO;
using System.Linq;
using GrapeCity.Documents.Html;

namespace ShortReportGen
{
    public class Html2Image
    {
        #region property definition  
        public struct Coordinates
        {
            public double lon;
            public double lat;
        }

        #region property members definition
        #endregion

        #region consts members definition
        public readonly string c_idkey = "id";
        public readonly string c_classkey = "class";
        public readonly string c_PngExt = ".jpg";
        public readonly double Ecology = 0.15;
        public readonly double ERPercent = -0.08;
        public readonly double AirPercent = -0.06;
        public readonly double SoilStrndPercent = -0.16;
        public readonly double NoiseStrndPercent = -0.15;
        public readonly string c_SevertyEmpty = "EllipseSevereEmpty";
        public readonly string c_ReportPath = @"C:\OSM\data\Report\Banner\";
        #endregion

        #endregion

        #region public methods

        public string CustomizeReport(Coordinates coor, string customername, string address, double[] Indices, string mapfile)
        {
            string source = System.IO.File.ReadAllText("Resources\\HTMLReportTemplate.html");
            try
            {
                double indicesAvg = Indices.Take(5).Average();
                double indicesSum = Indices.Take(5).Sum();

                source = source.Replace("[CustomerName]", customername);
                DateTime datetime = DateTime.Now;
                source = source.Replace("[CurrentDate]", String.Format("{0:ddd, MMM d, yyyy}", datetime));
                source = source.Replace("[Address]", address);
                source = source.Replace("[Coordinates]", coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######"));
                source = source.Replace("[TotalIndex]", Indices[5].ToString("0.#"));
                string severitylevel = string.Empty;
                for (int i = 0; i < 5; i++)
                {
                    // let's set/init the charts values
                    
                    double pievalue = 0;
                    double barvalue = 0;
                    if (indicesSum != 0)
                        pievalue = (Indices[i] / indicesSum) * indicesAvg;

                    string severityFullNewText = string.Empty;
                    string severityFullOldText = string.Empty;
                    string severitylevelSuffix = string.Empty;
                    string severitylevelCategory = string.Empty;
                    if ((Indices[i] >= 0) && (Indices[i] <= 3.5))
                    {
                        severitylevelSuffix = "Low";
                        severitylevel = "EllipseSevereLow";
                    }
                    else if ((Indices[i] > 3.5) && (Indices[i] <= 6.5))
                    {
                        severitylevelSuffix = "Mid";
                        severitylevel = "EllipseSevereMid";
                    }
                    else if ((Indices[i] > 6.5) && (Indices[i] <= 10))
                    {
                        severitylevelSuffix = "Heigh";
                        severitylevel = "EllipseSevereHigh";
                    }

                    switch (i)
                    {
                        case 0:
                            severitylevelCategory = "ElipseNoise";
                            barvalue = (NoiseStrndPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[NoisePieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[NoiseBarChartValue]", barvalue.ToString("0.##"));
                            break;
                        case 1:
                            severitylevelCategory = "ElipseSoil";
                            barvalue = (SoilStrndPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[SoilPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[SoilBarChartValue]", barvalue.ToString("0.##"));
                            break;
                        case 2:
                            severitylevelCategory = "ElipseER";
                            barvalue = (ERPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[ERPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[ERBarChartValue]", barvalue.ToString("0.##"));
                            break;
                        case 3:
                            severitylevelCategory = "ElipseAir";
                            barvalue = (AirPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[AirChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[AirBarChartValue]", barvalue.ToString("0.##"));
                            break;
                        case 4:
                            severitylevelCategory = "ElipseEcology";
                            barvalue = (Ecology / 10) * Indices[i] * 100;
                            source = source.Replace("[EcologyPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[EcologyBarChartValue]", barvalue.ToString("0.##"));
                            break;
                    }
                    severityFullOldText = c_idkey + "=\"" + severitylevelCategory + severitylevelSuffix + "\" " + c_classkey + "=\"" + c_SevertyEmpty + "\"";
                    severityFullNewText = c_idkey + "=\"" + severitylevelCategory + severitylevelSuffix + "\" " + c_classkey + "=\"" + severitylevel + "\"";
                    source = source.Replace(severityFullOldText, severityFullNewText);

                    
                }
                //  "file:///C:/Users/jamil-g/Desktop/map.jpg"
                source = source.Replace("[Map]", "file:///" + mapfile.Replace(@"\", "/"));
                string reportfile = c_ReportPath + Path.GetFileNameWithoutExtension (customername + "_" + ((DateTimeOffset)datetime).ToUnixTimeSeconds().ToString()) + c_PngExt;
                SaveHtmlToImage(source, reportfile);
                return (reportfile);
            }
            catch (Exception ex)
            {
                return (string.Empty);
            }
        }
        #endregion

        #region private methods
        private string SaveHtmlToImage(string htmlsource, string exportedfile)
        {
            try
            {
                // let's configure image settings
                var jpegSettings = new JpegSettings();
                jpegSettings.WindowSize = new Size(1443, 1100);
                jpegSettings.CompressionQuality = 100;

                // let's save HTML page to jpeg image 
                using (var htmlRenderer = new GcHtmlRenderer(htmlsource))
                {
                    htmlRenderer.RenderToJpeg(exportedfile, jpegSettings);
                }
                return exportedfile;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        #endregion
    }
}
