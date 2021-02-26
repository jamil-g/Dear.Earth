using System;
using log4net;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using OSM.ExportMapImage;
using Microsoft.Office.Interop.Word;
using Nominatim.API.Geocoders;
using Nominatim.API.Models;


namespace WordFinadReplaceNet
{
    public class GenerateReport : IGenerateReport
    {
        #region properties and members definition

        private const string wordExnt = ".docx";
        private const string PdfExnt = ".pdf";
        private readonly string StringSeparator = ";";

        private readonly string pdfReportPath = @"C:\OSM\data\Report\Reports\Pdf";
        private readonly string pdfReportDownlaodPath = @"C:\inetpub\wwwroot\AGER\ExtractOSMMapProd\Reports";
        private readonly string docxReportPath = @"C:\OSM\data\Report\Reports\Docx";
        private readonly string templateDoc = @"C:\OSM\data\Report\Template\ReportTemplate.docx";

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        #region structures definition

        public enum Types
        {
            Noise, Soil, Radiation, Ecology, AirQuality
        }

        public struct wikidata
        {
            public int key;
            public string value;
        }
        public struct Marks
        {
            public float NoiseInterference;
            public float SoilPollution;
            public float RadiationEG;
            public float AirQuality;
            public float Ecology;
            public float FinalMark;
        }

        public struct Coordinates
        {
            public float lon;
            public float lat;
            public int zoomlevel;
        }

        public struct ReportExtraInfo
        {
            public string ProjName;
            public string CustomerName;

        }
        #endregion

        #region public methods

        public string GenerateReportPdf(Marks marks, Coordinates coor, ReportExtraInfo reportextrainfo)
        {
            try
            {
                string serial = Guid.NewGuid().ToString();
                string FinalAddressAndReportName = string.Empty;
                string WordReport = docxReportPath + Path.DirectorySeparatorChar + serial + wordExnt;
                File.Copy(templateDoc, WordReport, true);
                Application wordApp = new Application { Visible = true };
                Document aDoc = wordApp.Documents.Open(WordReport, ReadOnly: false, Visible: true);
                aDoc.Activate();
                FindAndReplaceMainDoc(wordApp, "[Date]", DateTime.Now.ToString("dd/MM/yyyy"));
                FindAndReplaceMainDoc(wordApp, "[Serial_Number]", serial);
                FindAndReplaceMainDoc(wordApp, "[Project Name]", reportextrainfo.ProjName);
                FindAndReplaceMainDocHeaderAndFooter(aDoc, "[Project Name]", reportextrainfo.ProjName);
                FindAndReplaceMainDoc(wordApp, "[Company Name]", reportextrainfo.CustomerName);
                FindAndReplaceMainDoc(wordApp, "[Title]", "Indexes Value of - Coordinates " + Math.Round(coor.lon, 4) + "/" +
                                        Math.Round(coor.lat, 4));
                FindAndReplaceMainDoc(wordApp, "[Coordinates]", "Coordinates " + Math.Round(coor.lon, 4) + "/" +
                                        Math.Round(coor.lat, 4));

                // let's get the full address of the location according to the coordinates using OSM reverse geocoding service
                GeoCodingServices service = new GeoCodingServices();

                AddressResult address = service.ReverseGeoCoding(coor.lon, coor.lat);

                string country = string.IsNullOrEmpty(address.Country) ? "" : Convert.ToString(address.Country);
                string city = string.IsNullOrEmpty(address.City) ? "" : ", " + Convert.ToString(address.City);
                string road = string.IsNullOrEmpty(address.Road) ? "" : ", " + Convert.ToString(address.Road);
                string housenumber = string.IsNullOrEmpty(address.HouseNumber) ? "" : ", " + Convert.ToString(address.HouseNumber);
                string postalcode = address.PostCode=="no" ? "" : ", " + Convert.ToString(address.PostCode);
                string Fulladdress = country + city + road + housenumber + postalcode;
                if (!string.IsNullOrEmpty(Fulladdress))
                {
                    FinalAddressAndReportName += StringSeparator + "Address:" + Fulladdress ;
                    FindAndReplaceMainDoc(wordApp, "[Address]", Fulladdress);
                    FindAndReplaceMainDoc(wordApp, "[Site Name]", Fulladdress);

                    // if the city name exist, then let's check if the city name have value in wikipedia or not
                    if (!string.IsNullOrEmpty(address.City))
                    {
                        wikidata data = GetDataFromWiki(address.City);
                        if (data.key != -1)
                        {
                            object oBookMark = "SiteInfo";
                            aDoc.Bookmarks.get_Item(ref oBookMark).Range.Text = data.value;
                        }
                        else
                        {
                            FindAndReplaceMainDoc(wordApp, "[Site Brief Info]", " ");
                            FindAndReplaceMainDoc(wordApp, "Source: https://en.wikipedia.org/", " ");
                        }
                    }
                }
                else
                {
                    FinalAddressAndReportName += StringSeparator + "Address:" + " ";
                    FindAndReplaceMainDoc(wordApp, "[Address]", " ");
                    FindAndReplaceMainDoc(wordApp, "[Site Name]", " ");
                    FindAndReplaceMainDoc(wordApp, "[Site Brief Info]", " ");
                    FindAndReplaceMainDoc(wordApp, "Source: https://en.wikipedia.org/", " ");
                }

                // let's get the site 
                
                
                FindAndReplaceMainDoc(wordApp, "{EG}", Math.Round(marks.RadiationEG, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{EG}", Math.Round(marks.RadiationEG, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[EG Comments]", CheckinterferenceValue(Types.Radiation,Math.Round(marks.RadiationEG, 2)));
                FindAndReplaceMainDoc(wordApp, "{SC}", Math.Round(marks.SoilPollution, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{SC}", Math.Round(marks.SoilPollution, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[SC Comments]", CheckinterferenceValue(Types.Soil, Math.Round(marks.SoilPollution, 2)));
                FindAndReplaceMainDoc(wordApp, "{EC}", Math.Round(marks.Ecology, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{EC}", Math.Round(marks.Ecology, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[EC Comments]", CheckinterferenceValue(Types.Ecology, Math.Round(marks.Ecology, 2)));
                FindAndReplaceMainDoc(wordApp, "{NO}", Math.Round(marks.NoiseInterference, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{NO}", Math.Round(marks.NoiseInterference, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[NO Comments]", CheckinterferenceValue(Types.Noise, Math.Round(marks.NoiseInterference, 2)));
                FindAndReplaceMainDoc(wordApp, "{AQ}", Math.Round(marks.AirQuality, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{AQ}", Math.Round(marks.AirQuality, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[AO Comments]", CheckinterferenceValue(Types.AirQuality, Math.Round(marks.AirQuality, 2)));

                // will be change in the future to Solid Waste Management Mark and comments
                FindAndReplaceMainDoc(wordApp, "{SW}", Math.Round(marks.SoilPollution, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{SW}", Math.Round(marks.SoilPollution, 2).ToString());
                FindAndReplaceMainDoc(wordApp, "[SW Comments]", " ");

                // overall index value
                FindAndReplaceMainDoc(wordApp, "{M}", Math.Round(marks.FinalMark, 2).ToString());
                FindAnndReplaceShapes(aDoc, "{M}", Math.Round(marks.FinalMark, 2).ToString());
                AddMap(aDoc, coor);

                //string WordReport = docxReportPath + Path.DirectorySeparatorChar + serial + wordExnt;
                //wordApp.Documents[1].SaveAs2(WordReport, WdSaveFormat.wdFormatDocumentDefault);
                wordApp.Documents[1].Save();
                string pdfReport = pdfReportPath + Path.DirectorySeparatorChar + serial + PdfExnt;
                wordApp.Documents[1].SaveAs2(pdfReport, WdSaveFormat.wdFormatPDF);
                wordApp.Documents[1].Close();
                wordApp.Quit();
                int res = System.Runtime.InteropServices.Marshal.ReleaseComObject(aDoc);
                int res1 = System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                GC.Collect();

                // let's copy the file to authenticate folder for user downloads.
                FinalAddressAndReportName += StringSeparator + "Report:" + serial + PdfExnt;
                string downloadFile = pdfReportDownlaodPath + Path.DirectorySeparatorChar + serial + PdfExnt;
                File.Copy(pdfReport, downloadFile, true);

                return FinalAddressAndReportName;
            }
            catch (Exception e)
            {
                log.Error(e);
                return String.Empty;
            }

        }
        #endregion

        #region private methods

        private static wikidata GetDataFromWiki(string sitename)
        {
            wikidata data = new wikidata();
            string WikiaddressSuffix = "https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exlimit=max&explaintext&exintro&titles=";
            try
            {
                WebClient client = new WebClient();
                string address = WikiaddressSuffix + sitename;
                var response = client.DownloadString(address);

                var responseJson = JsonConvert.DeserializeObject<WikipediaClass.RootObject>(response);
                data.key = Convert.ToInt32(responseJson.query.pages.First().Key);
                data.value = responseJson.query.pages[data.key.ToString()].extract.ToString();
                return data;
            }
            catch (Exception e)
            {
                log.Error(e);
                return data;
            }
        }

        private static string CheckinterferenceValue(Types interferType, double value)
        {
            string comment = string.Empty;
            switch (interferType)
            {
                case Types.Radiation:
                    {
                        if (value>2.5 && value<=8.5)
                        {
                            comment = "Further checks of Radiation expert are needed due to adjacent facilities";
                        }
                        else if (value>=0 && value<=2.5)
                        {
                            comment = "We highly recommend contacting a local radiation expert";
                        }
                        else
                        {
                            comment = " ";
                        }
                        break;
                    }
                case Types.Soil:
                    if (value >= 7.5 && value <= 10)
                    {
                        comment = "potential soil contamination";
                    }
                    else if (value >= 2.5 && value <= 5)
                    {
                        comment = "We recommend contacting a local soil contamination expert";
                    }
                    else
                    {
                        comment = " ";
                    }
                    break;
                case Types.Ecology:
                    if (value >= 0 && value <= 2.5)
                    {
                        comment = "We recommend contacting a local Ecology expert";
                    }
                    else
                    {
                        comment = " ";
                    }
                    break;

                case Types.Noise:
                    if (value > 2.5 && value <= 4)
                    {
                        comment = "Noise pollution due to adjacent operations";
                    }
                    else if (value >= 0 && value <= 2.5)
                    {
                        comment = "We recommend contacting a local acoustic expert";
                    }
                    else
                    {
                        comment = " ";
                    }
                    break;

                case Types.AirQuality:
                    if (value >= 0 && value <= 4)
                    {
                        comment = "Air pollution due to adjacent operations";
                    }
                    else
                    {
                        comment = " ";
                    }
                    break;
            }
            return comment;
        }
        private static void FindAndReplaceMainDoc(Application doc, object findText, object replaceWithText)
        {
            try
            {
                //options
                object matchCase = false;
                object matchWholeWord = true;
                object matchWildCards = false;
                object matchSoundsLike = false;
                object matchAllWordForms = false;
                object forward = true;
                object format = false;
                object matchKashida = false;
                object matchDiacritics = false;
                object matchAlefHamza = false;
                object matchControl = false;
                object replace = 2;
                object wrap = 1;
                //execute find and replace
                doc.Selection.Find.Execute(ref findText, ref matchCase, ref matchWholeWord,
                    ref matchWildCards, ref matchSoundsLike, ref matchAllWordForms, ref forward, ref wrap, ref format, ref replaceWithText, ref replace,
                    ref matchKashida, ref matchDiacritics, ref matchAlefHamza, ref matchControl);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        private static void FindAndReplaceMainDocHeaderAndFooter(Document doc, object findText, object replaceWithText)
        {
            // this function find and replace text in the docuemtn header
            // it can easiley do the same for the document footer using section.footers like we use section.Headers
            try
            {
                foreach (Section section in doc.Sections)
                {
                    //options
                    object matchCase = false;
                    object matchWholeWord = true;
                    object matchWildCards = false;
                    object matchSoundsLike = false;
                    object matchAllWordForms = false;
                    object forward = true;
                    object format = false;
                    object matchKashida = false;
                    object matchDiacritics = false;
                    object matchAlefHamza = false;
                    object matchControl = false;
                    object replace = 2;
                    object wrap = 1;

                    Range HeaderRange = section.Headers[WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                    HeaderRange.Find.Execute(ref findText, ref matchCase, ref matchWholeWord,
                    ref matchWildCards, ref matchSoundsLike, ref matchAllWordForms, ref forward, ref wrap, ref format, ref replaceWithText, ref replace,
                    ref matchKashida, ref matchDiacritics, ref matchAlefHamza, ref matchControl);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }


        private static void FindAnndReplaceShapes(Document aDoc, string findText, string replaceWithText)
        {
            var shapes = aDoc.Shapes;

            foreach (Shape shape in shapes)
            {
                try
                {
                    if (shape.TextFrame.HasText != 0)
                    {
                        var initialText = shape.TextFrame.TextRange.Text;
                        var resultingText = initialText.Replace(findText, replaceWithText);
                        shape.TextFrame.TextRange.Text = resultingText;
                    }
                }
                catch (Exception e)
                {
                    log.Error(e);
                }
            }

        }

        private static void AddMap(Document aDoc, Coordinates coor)
        {
            try
            {
                ExportMapImage exportmap = new ExportMapImage();
                string mapLayoutFile = exportmap.ExportImage(coor.lon, coor.lat, coor.zoomlevel);
                if (mapLayoutFile != String.Empty)
                {
                    object oBookMark = "Map";
                    Object oMissed = aDoc.Bookmarks.get_Item(ref oBookMark).Range; //the position you want to insert
                    Object oLinkToFile = false;
                    Object oSaveWithDocument = true;
                    aDoc.Bookmarks.get_Item(ref oBookMark).Range.Text = string.Empty;
                    aDoc.InlineShapes.AddPicture(mapLayoutFile, ref oLinkToFile, ref oSaveWithDocument, ref oMissed);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
        #endregion
    }
}
