using System;
using Nominatim.API.Models;
using Nominatim.API.Geocoders;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using EnumsNET;
using OSM.ExportMapImage;
using ShortReportGen;
using ShortReportGen.Models;
using System.Linq;
using System.Threading.Tasks;
using shortid;
using ExtractOSMMapProd.Models;
using System.Data;
using System.Text;
using System.Reflection;
//using WordFinadReplaceNet;

namespace ExtractOSMMapProd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExtractOSMMapController : ControllerBase
    {
        #region ExtractOSMMapController definition  

        public struct Coordinates
        {
            public double lon;
            public double lat;
            public double zoomLevel;
        }

        public enum Types
        {
            [Description("All")]
            All,
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

        public struct Results
        {
            public Types type;
            public int category;
            public double indexvalue;
        }

        #region propoerty members definition

        //osm extraction varibales
        private string m_refno;
        private Coordinates coor;
        private double m_BackgroundInterference;
        private SQLDataClass dataClass;
        private string tblprefix = string.Empty;
        private ILoggerFactory loggerFactory = null;
        private ILogger<ExtractOSMMapController> m_logger;
        List<string> listString = new List<string> { "_point", "_line", "_polygon" };
        #endregion


        #region consts members definition
        private readonly string StringDot = ".";
        private readonly string StringSeparator = ";";
        private readonly string EmailSender = "report@dera.earth";
        //private readonly string Connectionstr = "Server=127.0.0.1;Port=5432;Database=BirdEye;User Id=postgres;Password=ko24k3;";
        private readonly string Connectionstr = "Server=18.132.162.121;Port=5432;Database=Dera;User Id=postgres;Password=koki_7yate32;";
        private readonly string googldocurl = "https://docs.google.com/forms/d/e/1FAIpQLSeF3QqtZ-W-3TG7L5HEhhYinCgg-mve7PkWjVkWaT-Ow-wUAA/viewform?usp=sf_link";
        private readonly string OSMLandUseBaseURL = "https://www.dera.earth/osm/LULC/#";
        #endregion


        #endregion


        #region Restful functions
        [HttpGet("GetRawData")]
        public JsonStringResult Get(double lon, double lat, double Scale, string CalcType, bool DelCalcTables = true)
        { 
            //this function received the WS parameters
            //and extract the osm data according to it
            //and return the result per category as a raw data

            // let's avoid low scale calculation and quit the calculation with an alert message
            if (Scale > 1000)
            {
                return new JsonStringResult(Properties.Resources.BigScale);
            }

            coor = new Coordinates() { lon = lon, lat = lat, zoomLevel = Scale };
            string strContent = string.Empty;
            try
            {
                initLogger();

                // let's extract the osm data and insert it to the PGSQL DB as tables
                tblprefix = GetDataFromOSM(coor);

                // let's get set background rank according to the coordinates geocoding (coordinates in a city, suburb, village and etc)
                m_BackgroundInterference = GetBackgroundRank(coor);

                dataClass = new SQLDataClass(Connectionstr);
                List<Attributes> attribLst = new List<Attributes>();
                attribLst = dataClass.LoadAttributes(dataClass.LoadCategory(Convert.ToString(CalcType)));

                List<CalcResults> resultsLst = new List<CalcResults>();
                foreach (Attributes item in attribLst)
                {
                    List<CalcResults> resultsLstTemp = dataClass.Getdata(tblprefix + listString[0], item, m_BackgroundInterference, coor);
                    if (resultsLstTemp != null)
                        resultsLst.AddRange(resultsLstTemp);
                    resultsLstTemp = dataClass.Getdata(tblprefix + listString[1], item, m_BackgroundInterference, coor);
                    if (resultsLstTemp != null)
                        resultsLst.AddRange(resultsLstTemp);
                    resultsLstTemp = dataClass.Getdata(tblprefix + listString[2], item, m_BackgroundInterference, coor);
                    if (resultsLstTemp != null)
                        resultsLst.AddRange(resultsLstTemp);
                }

                strContent = ConvertAttributesListtoTable(attribLst);
                strContent += ConvertResultListtoTable(resultsLst);
               

                if (DelCalcTables)
                {
                    dataClass.DeleteTables(tblprefix);
                }

            }
            catch (Exception ex)
            {
                strContent = ex.Message;
            }
         return new JsonStringResult(strContent);
        }

        [HttpGet("byCoordinate")]
        public JsonStringResult Get(double lon, double lat, double Scale, string CalcType, string Adminpwd, string ReportToken, string ProjectName, string CustomerName, string Recipients = "noemailaddr", bool DelCalcTables = true, bool RegHistory = true, string Lang = "EN_us")
        {
            //this function received the WS parameters
            //and extract the osm data according to it


            // let's avoid low scale calculation and quit the calculation with an alert message
            if (Scale > 1000)
            {
                return new JsonStringResult(Properties.Resources.BigScale);
            }

            coor = new Coordinates() { lon = lon, lat = lat, zoomLevel = Scale };
            string strContent = string.Empty;
            try
            {
                initLogger();

                // let's extract the osm data and insert it to the PGSQL DB as tables
                tblprefix = GetDataFromOSM(coor);

                // let's get set background rank according to the coordinates geocoding (coordinates in a city, suburb, village and etc)
                m_BackgroundInterference = GetBackgroundRank(coor);

                // convert string to enum
                Types type =((Types)Enum.Parse(typeof(Types), CalcType, true));

                // let generate new GUID as reference number or UUID in postgres lang
                m_refno = ShortId.Generate(true,false); //Guid.NewGuid();

                // let's calculate the attributes Indcies according to the coordinates location and other parameters
                dataClass = new SQLDataClass(Connectionstr);
                List<Results>  lstResults = dataClass.CalculateIndcies(tblprefix, coor, m_BackgroundInterference, ProjectName + StringDot + CustomerName, RegHistory, type, Recipients, m_refno);
                strContent += ParseData(lstResults);
                
                // let's get the address of teh location as a string and add it to the result JSON
                string Address = GetAddress(coor);
                strContent += StringSeparator + "Address:" + Address;

                // the report is disabled, later on will be added back to the results
                string reportName = StringSeparator + "Report:N.A.";
                strContent += reportName;

                if (DelCalcTables)
                {
                    dataClass.DeleteTables(tblprefix);
                }

                // let's calc the total index value and add it to the list for the short report and send it via email
                // only if the email recipients address was provided by the WS client
                if (Recipients != "noemailaddr")
                {
                    // let's create a new list to store the original values in the results list before adding factors to
                    // Noise & Air Quality index
                    List<Results> orgResultsLst = new List<Results>();
                    foreach (Results item in lstResults)
                        orgResultsLst.Add(item);

                    lstResults = SetIndicesFactor(lstResults, Types.Noise, 2);
                    lstResults = SetIndicesFactor(lstResults, Types.AirQuality, 2);
                    Results result = new Results { type = Types.All, category = 6, indexvalue = lstResults.Sum(x => x.indexvalue)/7 };
                    orgResultsLst.Add(result);
                    // let's send the shorty report email and update the email send status
                    strContent += StringSeparator + "Email:" + GenerateShortReportAsync(coor, Address, CustomerName, ProjectName, Recipients, orgResultsLst, Lang).Result;
                }
                // ** report code - disabled at this stage **
                //if (!string.IsNullOrEmpty(ReportToken) && ReportToken == "Report56562")
                //{
                //GenerateReport generateReport = new GenerateReport();
                //GenerateReport.Coordinates coordinates = new GenerateReport.Coordinates { lon = Convert.ToSingle(lon), lat = Convert.ToSingle(lat), zoomlevel = 17 };
                //GenerateReport.Marks marks = new GenerateReport.Marks { AirQuality = AirQualityVal, Ecology = EcologyVal, NoiseInterference = noiseVal, RadiationEG = radiationVal, SoilPollution = soilVal, FinalMark = ((noiseVal + soilVal + radiationVal + AirQualityVal + EcologyVal) / 5) };
                //GenerateReport.ReportExtraInfo ReportExtraInfo = new GenerateReport.ReportExtraInfo { CustomerName = CustomerName, ProjName = ProjectName };
                //string reportName = generateReport.GenerateReportPdf(marks, coordinates, ReportExtraInfo);";
                //strContent += reportName;
                //}
                //else
                //{
                //    strContent += "N.A.";
                //}

            }
            catch (Exception ex)
            {
                strContent = ex.Message;
            }

            return new JsonStringResult(strContent);
        }
        #endregion
        #region private methods
        private void initLogger()
        {
            // this function initialaize the class logger
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddEventLog();
            });
            m_logger = loggerFactory.CreateLogger<ExtractOSMMapController>(); ;
        }

        private async Task<string> GenerateShortReportAsync(Coordinates coord, string address, string customer, string project, string recipients, List<Results> lstResults, string Lang)
        {
            string content = string.Empty;
            try
            {
                // let's export the map to add it to the short report in zoom level const 17
                
                // will had change the use of map export with google maps api instead
                //ExportMapImage exportmap = new ExportMapImage();
                string mapLayoutFile = string.Empty;//exportmap.ExportImage(coord.lon, coord.lat, 17);

                Html2Image html2Image = new Html2Image();
                Html2Image.Coordinates coor = new Html2Image.Coordinates { lon = coord.lon, lat = coord.lat };
                double[] arr = new double[6] { lstResults[0].indexvalue, lstResults[1].indexvalue, lstResults[2].indexvalue, lstResults[3].indexvalue, lstResults[4].indexvalue, lstResults[5].indexvalue };
                // let's create OSM Landuse URL to get the landuse pie in the ROI
                // 15 is a static zoom level and /0 is a static Z Value
                string LULCURL = OSMLandUseBaseURL + $"17/{coor.lon}/{coor.lat}/0/";
                string report = html2Image.CustomizeReport(coor, customer, project, address, arr, mapLayoutFile, m_refno, Lang, LULCURL);
                EmailInfo EmailInfo = new EmailInfo();
                EmailInfo.Sender = EmailSender;
                EmailInfo.Recipients = recipients;
                switch (Lang)
                {
                    case "EN_us":
                        EmailInfo.Subject = Properties.Resources.EmailSubjectEn + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######");
                        EmailInfo.EmailMsg = $"Dear {customer}, <br><br> Thank you for choosing D.E.R.A: Digital Environmental Risk Assessment. Remote environmental risk management system for the real estate sector.. <br>" +
                            $"Please find attached the requested information on the location: {address} - Coordinates:[" + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######") + "].<br>" +
                            //$"We would be grateful if you would kindly answer a short questionnaire about your experience, click <a href = '{googldocurl}'>here</a> if you would like to be first in line to get our full product upon release.<br><br>" +
                            Properties.Resources.EmailGreetingsEn +
                            "<img id=\"CompanyLogo\" title=\"The Company Logo\" src=\"https://www.dera.earth/ExtractOSMMapProd/Markers/DeraEarthSignatureLogo.png\" </img>";
                        break;
                    case "FR_fr":
                        EmailInfo.Subject = Properties.Resources.EmailSubjectFr + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######");
                        EmailInfo.EmailMsg = $"Cher {customer}, <br><br> Merci d'avoir choisi IDZ CONSULTING: Évaluation numérique des risques environnementaux. Système de gestion à distance des risques environnementaux pour le secteur immobilier.. <br>" +
                            $"Veuillez trouver ci-joint les informations demandées sur le lieu : {address} - Coordonnées:[" + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######") + "].<br>" +
                            //$"We would be grateful if you would kindly answer a short questionnaire about your experience, click <a href = '{googldocurl}'>here</a> if you would like to be first in line to get our full product upon release.<br><br>" +
                            Properties.Resources.EmailGreetingsFr +
                            "<img id=\"CompanyLogo\" title=\"Le logo de l'entreprise\" src=\"https://www.dera.earth/wp-content/uploads/2021/01/IDZLogo.png\" </img>";
                        break;
                    default:
                        EmailInfo.Subject = Properties.Resources.EmailSubjectEn + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######");
                        EmailInfo.EmailMsg = $"Dear {customer}, <br><br> Thank you for choosing D.E.R.A: Digital Environmental Risk Assessment. Remote environmental risk management system for the real estate sector.. <br>" +
                            $"Please find attached the requested information on the location: {address} - Coordinates:[" + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######") + "].<br>" +
                            //$"We would be grateful if you would kindly answer a short questionnaire about your experience, click <a href = '{googldocurl}'>here</a> if you would like to be first in line to get our full product upon release.<br><br>" +
                            Properties.Resources.EmailGreetingsEn +
                            "<img id=\"CompanyLogo\" title=\"The Company Logo\" src=\"https://www.dera.earth/ExtractOSMMapProd/Markers/DeraEarthSignatureLogo.png\" </img>";
                        break;
                }
                EmailInfo.Attachment = report;
                StmpEmail email = new StmpEmail();
                await email.SendEmailAsync(EmailInfo);
                content = "Email Sent";
            }
            catch (Exception ex)
            {
                content = ex.Message;
            }
            return content;
        }

        private string ParseData(List<Results> lstResults)
        {
            // this function parse the calculation results from list to string
            string content = string.Empty;
            try
            {
                int counter = 0;
                foreach (Results item in lstResults)
                {
                    if (counter > 0)
                    {
                        content += StringSeparator;
                    }
                    content += ((Types)item.type).AsString(EnumFormat.Description) +  ":" + Math.Round(item.indexvalue,3).ToString();
                    counter++;
                }
            }
            catch (Exception ex)
            {
                content = ex.Message;
            }
            return content;
        }
        private List<Results> SetIndicesFactor(List<Results> lstResults, Types type, double factor)
        {
            try
            {
                int index = lstResults.FindIndex(x => x.type == type);
                if (index >= 0)
                {
                    // let's double the noise value to calculate accurate total index.
                    Results IndexUpdate = lstResults[index];
                    lstResults.Remove(IndexUpdate);
                    IndexUpdate.indexvalue = IndexUpdate.indexvalue * factor;
                    lstResults.Insert(index, IndexUpdate);
                }
            }
            catch (Exception ex)
            {
            }
            return lstResults;
        }
        private string GetAddress(Coordinates coor)
        {
            // this function get the address according to the coordinates (geocoding)
            string content = "";
            try
            {
                GeoCodingServices service = new GeoCodingServices();
                AddressResult address = service.ReverseGeoCoding(coor.lon, coor.lat);

                string country = string.IsNullOrEmpty(address.Country) ? "" : Convert.ToString(address.Country);
                string city = string.IsNullOrEmpty(address.City) ? "" : ", " + Convert.ToString(address.City);
                string road = string.IsNullOrEmpty(address.Road) ? "" : ", " + Convert.ToString(address.Road);
                string housenumber = string.IsNullOrEmpty(address.HouseNumber) ? "" : ", " + Convert.ToString(address.HouseNumber);
                string postalcode = address.PostCode == "no" ? "" : ", " + Convert.ToString(address.PostCode);
                string Fulladdress = country + city + road + housenumber + postalcode;
                if (!string.IsNullOrEmpty(Fulladdress))
                {
                    content=Fulladdress;
                }
            }
            catch (Exception ex)
            {
                content = ex.Message;
            }
            return content;
        }

        private string GetDataFromOSM(Coordinates coor)
        {
            try
            {
                // let's extract the osm data and insert it to the PGSQL DB as tables
                ExtractOSMMap OSMData = new ExtractOSMMap();
                tblprefix = OSMData.OsmFileDownload(coor.lon, coor.lat, coor.zoomLevel);
                return tblprefix;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private int GetBackgroundRank(Coordinates coor)
        {
            // let's get set background rank according to the coordinates geocoding (coordinates in a city, suburb, village and etc)
            try
            {
                GeoCodingServices service = new GeoCodingServices();
                int Rank = service.GetCityRank(coor.lon, coor.lat);
                int BackgroundRank = 0;
                switch (Rank)
                {
                    case 1:
                        BackgroundRank = 36;
                        break;
                    case 2:
                        BackgroundRank = 20;
                        break;
                    case 3:
                        BackgroundRank = 15;
                        break;
                    case 4:
                        BackgroundRank = 8;
                        break;
                    case 5:
                        BackgroundRank = 4;
                        break;
                    default:
                        BackgroundRank = 18;
                        break;
                }
                return BackgroundRank;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        private string ConvertResultListtoTable (List<CalcResults> lst)
        {
            DataTable DT = new DataTable();

            PropertyInfo[] properties = typeof(CalcResults).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                DT.Columns.Add(property.Name, property.PropertyType);
            }

            foreach (CalcResults item in lst)
            {
                DataRow row;
                row = DT.NewRow();
                foreach (PropertyInfo property in properties)
                {
                    row[property.Name] = property.GetValue(item);
                }
                DT.Rows.Add(row);
            }

            return ConvertDataTableToString(DT);
        }

        private string ConvertAttributesListtoTable(List<Attributes> lst)
        {
            DataTable DT = new DataTable();

            PropertyInfo[] properties = typeof(Attributes).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                DT.Columns.Add(property.Name, property.PropertyType);
            }

            foreach (Attributes item in lst)
            {
                DataRow row;
                row = DT.NewRow();
                foreach (PropertyInfo property in properties)
                {
                    row[property.Name] = property.GetValue(item);
                }
                DT.Rows.Add(row);
            }

            return ConvertDataTableToString(DT);
        }
        private string ConvertDataTableToString(DataTable dataTable)
        {
            var output = new StringBuilder();

            var columnsWidths = new int[dataTable.Columns.Count];

            // Get column widths
            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var length = row[i].ToString().Length;
                    if (columnsWidths[i] < length)
                        columnsWidths[i] = length;
                }
            }

            // Get Column Titles
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var length = dataTable.Columns[i].ColumnName.Length;
                if (columnsWidths[i] < length)
                    columnsWidths[i] = length;
            }

            // Write Column titles
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var text = dataTable.Columns[i].ColumnName;
                if (i>0)
                    output.Append("," + PadCenter(text, columnsWidths[i] + 2));
                else
                    output.Append(PadCenter(text, columnsWidths[i] + 2));
            }
            output.Append("\n");

            // Write Rows
            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var text = row[i].ToString();
                    if (i > 0)
                        output.Append("," + PadCenter(text, columnsWidths[i] + 2));
                    else
                        output.Append(PadCenter(text, columnsWidths[i] + 2));
                }
                output.Append("\n");
            }
            return output.ToString();
        }

        private string PadCenter(string text, int maxLength)
        {
            int diff = maxLength - text.Length;
            return new string(' ', diff / 2) + text + new string(' ', (int)(diff / 2.0 + 0.5));
        }
        #endregion
    }
    }

