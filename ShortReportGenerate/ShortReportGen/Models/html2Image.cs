﻿using System;
using System.IO;
using System.Linq;
using System.Drawing;
using ShortReportGenerate;
using System.ComponentModel;
using GrapeCity.Documents.Html;
using System.Collections.Generic;
using log4net;

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
        public readonly string c_PngExt = ".png";
        public readonly double Ecology = 0.15;
        public readonly double ERPercent = -0.08;
        public readonly double AirPercent = -0.06;
        public readonly double SoilStrndPercent = -0.16;
        public readonly double NoiseStrndPercent = -0.15;
        public readonly string c_CurrentStyle = "\" style=\"background-color:#F2F3F4;";
        public readonly string c_SevertyEmpty = "EllipseSevereEmpty";
        public readonly string c_ReportPath = @"C:\OSM\data\Report\Banner\";
        public readonly string c_ReserevedText = "© 2021 Dera Digital. All rights reserved";
        public readonly string c_UserConditions = "Please note that the using the current information is at the user own responsibility and subjected to the &nbsp; <a href='https://www.dera.earth/Terms/TermsOfService.pdf'> terms of use</a>";
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

        public string CustomizeReport(Coordinates coor, string customername, string project, string address, double[] Indices, string mapfile, string refno)
        {
            string source = System.IO.File.ReadAllText("Resources\\HTMLReportTemplate.html");
            try
            {
                // let's add the information of the compared sites indices in a List
                // this table should be created and moved to DB to allow a flexible data modifying
                string barColor = "'stroke-color: #000000; stroke-width: 2; fill-color: #FFFFFF'";
                List<Sites> lstSites = new List<Sites>
                { 
                    new Sites {siteName = "'Taj Mahal'", index=9.0f, style=barColor},
                    new Sites {siteName = "'Empire state'", index=7.4f, style=barColor},
                    new Sites {siteName = "'Niagara Falls'", index=8.6f, style=barColor},
                    new Sites {siteName = "'Shanghai Center'", index=7.4f, style=barColor},
                    new Sites {siteName = "'Luanda Center'", index=6.6f, style=barColor}
                };
                
                // add the current location index
                Sites site = new Sites { siteName = "'You'", index = (float)Math.Round(Indices[5],1), style = "'red'" };
                lstSites.Add(site);

                // sort the list by the index
                List<Sites> listSitesOrd =  lstSites.OrderBy(s => s.index).ToList();

                // lte's compose the string that match the google chart bar rows format
                string siteComp = string.Empty;
                foreach (var item in listSitesOrd)
                {
                    siteComp += "[" + item.siteName + "," + item.index + "," + item.style + "]\n,";
                }
                siteComp = siteComp.Substring(0, siteComp.Length - 1);

                double indicesAvg = Indices.Take(5).Average();
                double indicesSum = Indices.Take(5).Sum();

                source = source.Replace("[CustomerName]", customername);
                source = source.Replace("[Company]", project);
                DateTime datetime = DateTime.Now;
                source = source.Replace("[CurrentDate]", String.Format("{0:ddd, MMM d, yyyy}", datetime));
                source = source.Replace("[Address]", address);
                source = source.Replace("[Coordinates]", coor.lat.ToString("0.########") + "," + coor.lon.ToString("0.########"));
                source = source.Replace("[TotalIndex]", Indices[5].ToString("0.#"));
                source = source.Replace("[RefNumber]", refno);
                source = source.Replace("[UseConditions]", c_UserConditions);
                source = source.Replace("[CurrentDateShort]", String.Format("{0:MM/dd//yyyy}", datetime));
                source = source.Replace("[Reserved Data]", c_ReserevedText);
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
                string reportfile = c_ReportPath + Path.GetFileNameWithoutExtension (customername + "_" + ((DateTimeOffset)datetime).ToUnixTimeSeconds().ToString()) + c_PngExt;
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
                // let's configure image settings
                var jpegSettings = new PngSettings();
                jpegSettings.WindowSize = new Size(1300, 1100);
               //jpegSettings.CompressionQuality = 500;
  
                // let's save HTML page to jpeg image 
                using (var htmlRenderer = new GcHtmlRenderer(htmlsource))
                {
                    htmlRenderer.VirtualTimeBudget = 3000;
                    htmlRenderer.RenderToPng(exportedfile, jpegSettings);
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
