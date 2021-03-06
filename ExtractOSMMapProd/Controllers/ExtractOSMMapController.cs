﻿using System;
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
        private Coordinates coor;
        private double m_BackgroundInterference;
        private SQLDataClass dataClass;
        private string tblprefix = string.Empty;
        private ILoggerFactory loggerFactory = null;
        private ILogger<ExtractOSMMapController> m_logger;
        #endregion


        #region consts members definition
        private readonly string StringDot = ".";
        private readonly string StringSeparator = ";";
        private readonly string EmailSender = "report@dera.earth";
        private readonly string Connectionstr = "Server=127.0.0.1;Port=5432;Database=BirdEye;User Id=postgres;Password=ko24k3;";
        #endregion


        #endregion


        #region Restful functions
        [HttpGet("byCoordinate")]
        public JsonStringResult Get(double lon, double lat, double Scale, string CalcType, string Adminpwd, string ReportToken, string ProjectName, string CustomerName, string Recipients, bool DelCalcTables = true)
        {
            //this function received the WS parameters
            //and extract the osm data according to it


            // let's avoid low scale calculation and quit the calculation with an alert message
            if (Scale > 0.5)
            {
                return new JsonStringResult(ExtractOSMMapProdProd.Properties.Resources.BigScale);
            }

            coor = new Coordinates() { lon = lon, lat = lat, zoomLevel = Scale };
            string strContent = string.Empty;
            try
            {
                initLogger();

                // let's extract the osm data and insert it to the PGSQL DB as tables
                ExtractOSMMap OSMData = new ExtractOSMMap();
                tblprefix = OSMData.OsmFileDownload(lon, lat, Scale);
               
                // let's get set background rank according to the coordinates geocoding (coordinates in a city, suburb, village and etc)
                GeoCodingServices service = new GeoCodingServices();
                int Rank = service.GetCityRank(coor.lon, coor.lat);

                switch (Rank)
                {
                    case 1:
                        m_BackgroundInterference = 36;
                        break;
                    case 2:
                        m_BackgroundInterference = 20;
                        break;
                    case 3:
                        m_BackgroundInterference = 15;
                        break;
                    case 4:
                        m_BackgroundInterference = 8;
                        break;
                    case 5:
                        m_BackgroundInterference = 4;
                        break;
                    default:
                        m_BackgroundInterference = 0;
                        break;
                }

                // convert string to enum
                Types type =((Types)Enum.Parse(typeof(Types), CalcType, true));

                // let's calculate the attributes Indcies according to the coordinates location and other parameters
                dataClass = new SQLDataClass(Connectionstr);
                List<Results>  lstResults = dataClass.CalculateIndcies(tblprefix, coor, m_BackgroundInterference, ProjectName + StringDot + CustomerName, true, type);
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

                // let's calc the total index value and add it to the list
                Results result = new Results { type = Types.All, category = 6, indexvalue = lstResults.Average(x => x.indexvalue) };
                lstResults.Add(result);
                // let's send the shorty report email and update the email send status
                strContent += StringSeparator + "Email:" + GenerateShortReport(coor, Address, CustomerName, Recipients, lstResults);

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

        private string GenerateShortReport(Coordinates coord, string address, string customer, string recipients, List<Results> lstResults)
        {
            string content = string.Empty;
            try
            {
                // let's export the map to add it to the short report in zoom level const 17
                ExportMapImage exportmap = new ExportMapImage();
                string mapLayoutFile = exportmap.ExportImage(coord.lon, coord.lat, 17);

                Html2Image html2Image = new Html2Image();
                Html2Image.Coordinates coor = new Html2Image.Coordinates { lon = coord.lon, lat = coord.lat };
                double[] arr = new double[6] { lstResults[0].indexvalue, lstResults[1].indexvalue, lstResults[2].indexvalue, lstResults[3].indexvalue, lstResults[4].indexvalue, lstResults[5].indexvalue };
                string report = html2Image.CustomizeReport(coor, customer, address, arr, mapLayoutFile);
                EmailInfo EmailInfo = new EmailInfo();
                EmailInfo.Sender = EmailSender;
                EmailInfo.Recipients = recipients;
                EmailInfo.Subject = $"Dear.Earth Indcies report of location: {address} - " + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######");
                EmailInfo.EmailMsg = $"Dear {customer}, <br><br> Thank you for choosing Dear.Earth to Calculate your environmental Indcies. <br>" +
                    $"Please find attached the requested information of location: {address} - Coordinates:[" + coor.lon.ToString("0.######") + "," + coor.lat.ToString("0.######") + "].<br><br>" +
                    "Best Regards, <br> Dear.Earth Team <br>" +
                    "<img id=\"CompanyLogo\" title=\"The Company Logo\" src=\"https://www.ager.earth/ExtractOSMMapProd/Markers/DeraEarthSignatureLogo.png\" </img>";
                EmailInfo.Attachment = report;
                StmpEmail email = new StmpEmail();
                email.SendEmail(EmailInfo);
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
        #endregion
    }
}

