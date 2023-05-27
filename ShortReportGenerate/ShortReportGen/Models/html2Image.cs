using System;
using System.IO;
using System.Linq;
using System.Drawing;
using ShortReportGenerate;
using System.ComponentModel;
using GrapeCity.Documents.Html;
using System.Collections.Generic;
using log4net;
using GrapeCity.Documents.Pdf;

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

        public struct Sites
        {
            public string siteName;
            public float index;
            public string style;
            public string annotation;
        }

        public enum Types
        {
            [Description("Noise")]
            Noise,
            [Description("Soil")]
            Soil,
            [Description("Radiation")]
            Radiation,
            [Description("Air Quality")]
            AirQuality,
            [Description("Ecology")]
            Ecology
        }

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #region property members definition
        #endregion

        #region consts members definition
        public readonly string c_idkey = "id";
        public readonly string c_classkey = "class";
        public readonly string c_PdfExt = ".pdf";
        public readonly double Ecology = 0.15;
        public readonly double ERPercent = -0.08;
        public readonly double AirPercent = -0.06;
        public readonly double SoilStrndPercent = -0.16;
        public readonly double NoiseStrndPercent = -0.15;
        public readonly string c_CurrentStyle = "\" style=\"background-color:#F2F3F4;\"";
        public readonly string c_SevertyEmpty = "EllipseSevereEmpty";
        public readonly string c_ReportPath = @"C:\OSM\data\Report\Banner\";
        public string m_ReserevedText;// = "© 2021 Dera Digital LTD. All rights reserved";
        public string m_UserConditions;// = "Please note that any use of DERA and its contents is the sole responsibility of the user and subjected to the &nbsp; <a href='https://www.dera.earth/Terms/TermsOfService.pdf'> terms of use</a>";
        public readonly string c_GoogleMapsAPIKey = "AIzaSyC6ztpHBdjfDi0sGmyT62btVTQ2ckAQVjs";
        #endregion

        #endregion

        #region public methods

        public string generateEstateImg(double index)
        {
            try
            {
                string exportpath = @"C:\OSM\data\Report\Banner\RealEstateGraph";
                string filename = $@"{Guid.NewGuid()}" + ".png";
                Image ScaleEstateImg = Resource.EstateScaleBar; 
                Image mrkImg = Resource.EstateIndicator;
                string strIndicator = Math.Round(index,0).ToString() + "%";
                // if the index value is great than 0 let's add the + sign to the number string value
                if (Math.Sign(index) > 0)
                {
                    strIndicator = "+" + strIndicator;
                }

                PointF indicatorTextLocation = new PointF(20f, 25f);

                // if the index value includes 2 digits let's move the number location to the right so it in
                //  fits better the circle.
                if (Math.Abs(index)>=10)
                {
                    indicatorTextLocation = new PointF(15f, 25f);
                }

                // let's add the index to the curcle graphic
                using (Graphics graphics = Graphics.FromImage(mrkImg))
                {
                    using (Font txtFont = new Font("Britannic Bold", 14))
                    {
                        graphics.DrawString(strIndicator, txtFont, Brushes.White, indicatorTextLocation);
                    }
                }

                // let's calculate and locate the graphic circle with the index in the correct location 
                // along the Y axis according to the index value
                using (Graphics graphics = Graphics.FromImage(ScaleEstateImg))
                {
                    int factor = 11;
                    if (Math.Sign(index) == -1)
                        factor = 6;
                    int newHeight = 250 + (Convert.ToInt16(Math.Round(index, 0)) * factor) * -1;
                    graphics.DrawImage(mrkImg, 33.5f, newHeight);
                }
                string mapLayoutImage = exportpath + Path.DirectorySeparatorChar + filename;
                ScaleEstateImg.Save(mapLayoutImage);
                return mapLayoutImage;
            }
            catch (Exception e)
            {
                log.Error(e);
                return string.Empty;
            }
        }

        public string CustomizeReport(Coordinates coor, string customername, string project, string address, double[] Indices, string mapfile, string refno, string Lang)
        {
            string source = File.ReadAllText("Resources\\HTMLReportTemplate_EN_us.html");
            switch (Lang)
            {
                case ("EN_us"):
                    source = File.ReadAllText("Resources\\HTMLReportTemplate_EN_us.html");
                    break;
                case ("FR_fr"):
                    source = File.ReadAllText("Resources\\HTMLReportTemplate_FR_fr.html");
                    break;
            }
             
            try
            {
                // let's add the information of the compared sites indices in a List
                // this table should be created and moved to DB to allow a flexible data modifying
                string barColor = "'stroke-color: #000000; stroke-width: 2; fill-color: #FFFFFF'";
                List<Sites> lstSites = new List<Sites>
                { 
                    new Sites {siteName = "'King Cross'", index=3.3f, style=barColor, annotation="'3.3'"},
                    new Sites {siteName = "'Arc de Triomphe'", index=5.2f, style=barColor, annotation="'5.2'"},
                    new Sites {siteName = "'Big Ben'", index=6.0f, style=barColor, annotation="'6.0'"},
                    new Sites {siteName = "'Sistine Chapel'", index=7.4f, style=barColor, annotation="'7.4'"},
                    new Sites {siteName = "'Burj Khalifa'", index=8.5f, style=barColor, annotation="'8.5'"},
                    new Sites {siteName = "'Pyramids of Giza'", index=8.5f, style=barColor, annotation="'9.5'"}
                };

                // add the current location index
                string indexannotation = "'You'";
                switch (Lang)
                {
                    case ("EN_us"):
                        indexannotation = "'You'";
                        break;
                    case ("FR_fr"):
                        indexannotation = "'Toi'";
                        break;
                    default:
                        indexannotation = "'You'";
                        break;
                }
                Sites site = new Sites { siteName = "''", index = (float)Math.Round(Indices[5],1), style = "'color: #d3d3d3'", annotation= indexannotation };
                lstSites.Add(site);

                // sort the list by the index
                List<Sites> listSitesOrd =  lstSites.OrderBy(s => s.index).ToList();

                // lte's compose the string that match the google chart bar rows format
                string siteComp = string.Empty;
                foreach (var item in listSitesOrd)
                {
                    siteComp += "[" + item.siteName + "," + item.index + "," + item.style + "," + item.annotation +  "]\n,";
                }
                siteComp = siteComp.Substring(0, siteComp.Length - 1);

                //double indicesAvg = Indices.Take(5).Average();
                //double indicesSum = Indices.Take(5).Sum();
                string wrongAddr = "Palestinian Territory";
                string correctAddr = "Judea and Samaria";
                source = source.Replace("[CustomerName]", customername);
                source = source.Replace("[Company]", project);
                DateTime datetime = DateTime.Now;
                source = source.Replace("[CurrentDate]", String.Format("{0:ddd, MMM d, yyyy}", datetime));
                source = source.Replace("[Address]", address.Replace(wrongAddr, correctAddr));
                source = source.Replace("[Coordinates]", coor.lat.ToString("0.########") + "," + coor.lon.ToString("0.########"));
                source = source.Replace("[TotalIndex]", Indices[5].ToString("0.#"));
                source = source.Replace("[RefNumber]", refno);
                source = source.Replace("[CurrentDateShort]", String.Format("{0:MM/dd//yyyy}", datetime));
                switch (Lang)
                {
                    case ("EN_us"):
                        m_UserConditions = "Please note that any use of DERA and its contents is the sole responsibility of the user and subjected to the &nbsp; <a href = 'https://www.dera.earth/Terms/TermsOfService.pdf' > terms of use</a> ";
                        m_ReserevedText = "© 2021 Dera Digital LTD. All rights reserved";
                        break;
                    case ("FR_fr"):
                        m_UserConditions = "Veuillez noter que toute utilisation de DERA et de son contenu relève de la seule responsabilité de l'utilisateur et est soumise à la &nbsp; <a href = 'https://www.dera.earth/Terms/TermsOfService.pdf' > conditions d'utilisation</a> ";
                        m_ReserevedText = "© 2021 Dera Digital LTD. tous droits réservés";
                        break;
                    default:
                        m_UserConditions = "Please note that any use of DERA and its contents is the sole responsibility of the user and subjected to the &nbsp; <a href = 'https://www.dera.earth/Terms/TermsOfService.pdf' > terms of use</a> ";
                        m_ReserevedText = "© 2021 Dera Digital LTD. All rights reserved";
                        break;
                }
                source = source.Replace("[UseConditions]", m_UserConditions);
                source = source.Replace("[Reserved Data]", m_ReserevedText);
                source = source.Replace("[sitesComp]", siteComp);
                source = source.Replace("[ApiKey]", c_GoogleMapsAPIKey);
                string severitylevel = string.Empty;
                string severityColor = string.Empty;
                Dictionary<Types, float> dicEstateIndicators = new Dictionary<Types, float>
                {
                    {Types.Noise, -15.0f },
                    {Types.Soil, -16.0f },
                    {Types.AirQuality, -6.0f },
                    {Types.Radiation, -8.0f },
                    {Types.Ecology, 15.0f },
                };

                List<double> lstEstateIndices = new List<double>();
                for (int i = 0; i < 5; i++)
                {
                    // let's set/init the charts values
                    
                    //double pievalue = 0;
                    //double barvalue = 0;
                    //if (indicesSum != 0)
                    //    pievalue = (Indices[i] / indicesSum) * indicesAvg;

                    //string severityFullNewText = string.Empty;
                    //string severityFullOldText = string.Empty;
                    string severitylevelSuffix = string.Empty;
                    //string severitylevelCategory = string.Empty;
                    if (i < 4)
                    {
                        if ((Indices[i] >= 0) && (Indices[i] <= 2.5))
                        {
                            severitylevelSuffix = "Low";
                            severityColor = " style=\"background-color: Red; \"";
                            //severitylevel = "EllipseSevereLow";
                        }
                        else if ((Indices[i] > 2.5) && (Indices[i] <= 7.5))
                        {
                            severitylevelSuffix = "Medium";
                            severityColor = " style=\"background-color: Orange; \"";
                            //severitylevel = "EllipseSevereMid";
                        }
                        else if (Indices[i] > 7.5)
                        {
                            severitylevelSuffix = "Heigh";
                            severityColor = " style=\"background-color: Green; \"";
                            //severitylevel = "EllipseSevereHigh";
                        }
                    }
                    else if (i>3)
                    {
                        if ((Indices[i] >= 0) && (Indices[i] < 1.5))
                        {
                            severitylevelSuffix = "Low";
                            severityColor = " style=\"background-color: Red; \"";
                            //severitylevel = "EllipseSevereLow";
                        }
                        else if ((Indices[i] >= 1.5) && (Indices[i] <= 4.0))
                        {
                            severitylevelSuffix = "Medium";
                            severityColor = " style=\"background-color: Orange; \"";
                            //severitylevel = "EllipseSevereMid";
                        }
                        else if (Indices[i] > 4.0)
                        {
                            severitylevelSuffix = "Heigh";
                            severityColor = " style=\"background-color: Green; \"";
                            //severitylevel = "EllipseSevereHigh";
                        }
                    }


                    switch (i)
                    {
                        case 0:
                            severitylevel = "Noise" + severitylevelSuffix;
                            lstEstateIndices.Add(dicEstateIndicators[Types.Noise] / 10 * (10 - Indices[i]));
                           /* severitylevelCategory = "ElipseNoise";
                            barvalue = (NoiseStrndPercent / 10) * Indices[i] * 100;
                            if (barvalue >= 0)
                                source = source.Replace("[ColorNoise]", pievalue.ToString("Green"));
                            else
                                source = source.Replace("[ColorNoise]", pievalue.ToString("Red"));
                            source = source.Replace("[NoisePieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[NoiseBarChartValue]", barvalue.ToString("0.##"));*/
                            break;
                        case 1:
                            severitylevel = "Soil" + severitylevelSuffix;
                            lstEstateIndices.Add(dicEstateIndicators[Types.Soil] / 10 * (10 - Indices[i]));
                            /*severitylevelCategory = "ElipseSoil";
                            barvalue = (SoilStrndPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[SoilPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[SoilBarChartValue]", barvalue.ToString("0.##"));
                            if (barvalue >= 0)
                                source = source.Replace("[ColorSoil]", pievalue.ToString("Green"));
                            else
                                source = source.Replace("[ColorSoil]", pievalue.ToString("Red"));*/
                            break;
                        case 2:
                            severitylevel = "Radiation" + severitylevelSuffix;
                            lstEstateIndices.Add(dicEstateIndicators[Types.Radiation] / 10 * (10 - Indices[i]));
                            /*severitylevelCategory = "ElipseER";
                            barvalue = (ERPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[ERPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[ERBarChartValue]", barvalue.ToString("0.##"));
                            if (barvalue >= 0)
                                source = source.Replace("[ColorER]", pievalue.ToString("Green"));
                            else
                                source = source.Replace("[ColorER]", pievalue.ToString("Red"));*/
                            break;
                        case 3:
                            severitylevel = "AirQuality" + severitylevelSuffix;
                            lstEstateIndices.Add(dicEstateIndicators[Types.AirQuality] / 10 * (10 - Indices[i]));
                            /*severitylevelCategory = "ElipseAir";
                            barvalue = (AirPercent / 10) * Indices[i] * 100;
                            source = source.Replace("[AirChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[AirBarChartValue]", barvalue.ToString("0.##"));
                            if (barvalue >= 0)
                                source = source.Replace("[ColorAQ]", pievalue.ToString("Green"));
                            else
                                source = source.Replace("[ColorAQ]", pievalue.ToString("Red"));*/
                            break;
                        case 4:
                            severitylevel = "Ecology" + severitylevelSuffix;
                            lstEstateIndices.Add(dicEstateIndicators[Types.Ecology] / 10 * Indices[i]);
                            /*severitylevelCategory = "ElipseEcology";
                            barvalue = (Ecology / 10) * Indices[i] * 100;
                            source = source.Replace("[EcologyPieChartValue]", pievalue.ToString("0.##"));
                            source = source.Replace("[EcologyBarChartValue]", barvalue.ToString("0.##"));
                            if (barvalue >= 0)
                                source = source.Replace("[ColorEG]", pievalue.ToString("Green"));
                            else
                                source = source.Replace("[ColorEG]", pievalue.ToString("Red"));
                            break;*/
                            break;
                    }
                    //severityFullOldText = c_idkey + "=\"" + severitylevelCategory + severitylevelSuffix + "\" " + c_classkey + "=\"" + c_SevertyEmpty + "\"";
                    //severityFullNewText = c_idkey + "=\"" + severitylevelCategory + severitylevelSuffix + "\" " + c_classkey + "=\"" + severitylevel + "\"";
                    source = source.Replace(severitylevel + c_CurrentStyle, severitylevel + "\" " +  severityColor);
                }
                double SumEstateInd = lstEstateIndices.Sum();
                if (SumEstateInd > 20 || SumEstateInd  < -20)
                    SumEstateInd = 20 * Math.Sign(SumEstateInd);
                mapfile = generateEstateImg(SumEstateInd);
                
                //  "file:///C:/Users/jamil-g/Desktop/map.jpg"
                source = source.Replace("[EstateGraph]", "'file:///" + mapfile.Replace(@"\", "/") +"'");
                string reportfile = c_ReportPath + Path.GetFileNameWithoutExtension (customername + "_" + ((DateTimeOffset)datetime).ToUnixTimeSeconds().ToString()) + c_PdfExt;
                SaveHtmlToImage(source, reportfile);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"C:\OSM\data\Report\Template\Html", "template.html")))
                {
                    outputFile.WriteLine(source);
                }
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

                //html to pdf
                /*using (var re = new GcHtmlRenderer(htmlsource))
                {
                    //Define parameters for the PDF generator
                    var pdfSettings = new PdfSettings()
                    {
                        // Skip the first page which is basically empty
                        //PageRanges = "2-100",
                        // Sets the page width in inches
                        //PageWidth = 12f,
                        // Sets the page height in inches
                        //PageHeight = 11f,
                        // Sets page margins all around (default is no margins)
                        //Margins = new Margins(0f),
                        // Ignores the page size defined by CSS
                        //IgnoreCSSPageSize = false,
                        // Use landscape orientation to make sure long code lines are not truncated
                        Landscape = false,
                        //Sets the background color of the HTML page
                        //DefaultBackgroundColor = Color.Azure,
                        FullPage=true,
                        //WindowSize = new Size(1300, 800)
                };

                    //Create a PDF file from the source HTML
                    re.VirtualTimeBudget = 3000;
                    re.RenderToPdf(exportedfile, pdfSettings);
                }*/


                //Using A4+ in landscape, note that the width/height values are swapped  
                EO.Pdf.HtmlToPdf.Options.MinLoadWaitTime = 1000;
                EO.Pdf.HtmlToPdf.Options.PageSize = new SizeF(17f, 10f);
                EO.Pdf.HtmlToPdf.ConvertHtml(htmlsource, exportedfile);


                // let's configure image settings
                //var jpegSettings = new PngSettings();
                //jpegSettings.WindowSize = new Size(1300, 800);
                //jpegSettings.CompressionQuality = 500;

                // let's save HTML page to jpeg image 
                /*using (var htmlRenderer = new GcHtmlRenderer(htmlsource))
                {
                    htmlRenderer.VirtualTimeBudget = 3000;
                    //htmlRenderer.RenderToPng(exportedfile, jpegSettings);
                    string filePath = exportedfile;
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        htmlRenderer.RenderToPdf(fs, pdfSettings);
                    }

                }*/
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
