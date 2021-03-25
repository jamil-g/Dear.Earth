using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExtractOSMMapProd
{
    public class SoilInterference : BuilderClass
    {
        #region ExtractOSMMapController definition  

        #region propoerty members
        //osm extraction and filter configuration values
        private string m_strFileName = "";
        private string m_strOutputTag = " -o=";
        private string m_strOutputFolder = "Data";
        private string m_strFilterExec = "osmFilter.bat";
        private string m_strFilterFilteExt = "_filter.osm";
        private string m_strExtractExec = "Extract_Hw_services.bat";
        private string m_strExtractParam_tmp = " --keep=\"cat=subca\"";

        private Product m_Product = new Product();
        private readonly ILogger<SoilInterference> _logger;

        //these class member vriables are temoporary for QA/QC and results calibration use
        public double m_MinDecibels { set; get; } //= 35;
        public double m_MaxDecibels { set; get; } //= 95;
        public double m_MaxRange { set; get; } //= 10;
        public double m_MinRange { set; get; } //= 0;
        public double m_DecLocal { set; get; } //= 40;
        public double m_DecPrimary { set; get; } //= 65;
        public double m_DecHway { set; get; } //= 83;
        public double m_DecHJunction { set; get; } //= 50;
        public double m_DecHBusStop { set; get; } // = 50;
        public double m_MainPowerLine { set; get; } //= 50;
        public double m_MinorPowerLine { set; get; } //= 50;
        public Boolean m_IncludeTitle { set; get; }


        //until here should be removed after 
        private string m_strPath = @"C:\OSM\data\Converter";
        private string m_strFilterParam_tmp = "_filter.osm --all-to-nodes --csv=\"@id @lon @lat cat subca name\" --csv-headline --csv-separator=,";
        #endregion

        //url for downloading osm data as constant variable
        #region const variables
        const string m_strOSMURL = "https://api.openstreetmap.org/api/0.6/map?bbox=";
        #endregion
        #endregion


        #region public functions
        public SoilInterference(ILogger<SoilInterference> logger)
        {
            //logger function
            _logger = logger;
        }

        public override void CalculateLocationQuality(double lon, double lat, double Scale, string Category, string SubCategory, string Adminpwd)

        {
            //this function received the WS parameters
            //and extract the osm data according to it

            string strContent = "";
            try
            {
                //for QA/QC, should be removed later + the above function parameters accordingly
                /*
                m_MinDecibels = MinDecibels;
                m_MaxDecibels = MaxDecibels;
                m_MaxRange = MaxRange;
                m_MinRange = MinRange;
                m_DecLocal = DecLocal;
                m_DecPrimary = DecPrimary;
                m_DecHway = DecHway;
                m_DecHJunction = DecJunc;
                m_DecHBusStop = DecBStop;
                m_MainPowerLine = DecPowerMain;
                m_MinorPowerLine = DecPowerMainor;
                */

                //until here

                double Decibeles = 0;
                string temp1 = string.Empty;
                string temp2 = string.Empty;
                string strCSVtitle = string.Empty;
                string strFilterParam = string.Empty;
                string m_strExtractParam = string.Empty;

                //let's create and initialize the OSM data array 
                OsmData[] array = null;

                //if the map scale is too high, rejected the extract request
                if (Scale > 0.5)
                    strContent = ExtractOSMMapProdProd.Properties.Resources.BigScale;
                //if the category name is not empty and we could download the OSM file then we can 
                //process the osm file filtering
                else if (!string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(SubCategory) && OsmFileDownload(lon, lat, Scale))
                {
                    //let's retrieve the category string value and subcategory from
                    //the html page controller and exchange it with the initial
                    //temporary value
                    var strCatValues = Category.Split(',');
                    var strSubCatValues = SubCategory.Split(',');

                    int nCount = 0;
                    foreach (string items in strCatValues)
                    {
                        temp1 = m_strExtractParam_tmp.Replace("cat", strCatValues[nCount]);
                        temp2 = temp1.Replace("subca", strSubCatValues[nCount]);
                        m_strExtractParam = temp2;

                        //temp1 = m_strFilterParam_tmp.Replace("cat", Category);
                        //temp2 = temp1.Replace("subca", SubCategory);
                        //string m_strFilterParam = temp2;

                        //since we wil include more than 1 cat in the same osm file
                        //we revert back the Cat and Subca in th header (title)
                        strFilterParam = m_strFilterParam_tmp;

                        //run the osm filter extract command we run batch file because the execute
                        //file is unsigned and is not support by the Process command 
                        //(only singed execution files).
                        RunProcess(m_strPath, m_strFilterExec,
                                              m_strPath + System.IO.Path.DirectorySeparatorChar +
                                              m_strOutputFolder + System.IO.Path.DirectorySeparatorChar + m_strFileName + m_strExtractParam +
                                              m_strOutputTag + m_strPath + System.IO.Path.DirectorySeparatorChar +
                                              m_strOutputFolder + System.IO.Path.DirectorySeparatorChar + m_strFileName + m_strFilterFilteExt);
                        String results = RunProcess(m_strPath, m_strExtractExec,
                                                 m_strPath + System.IO.Path.DirectorySeparatorChar +
                                                 m_strOutputFolder + System.IO.Path.DirectorySeparatorChar + m_strFileName + strFilterParam);

                        //get the title and store it in string variable only from the 1st feltering file
                        int nChkResults = results.IndexOf(Environment.NewLine, results.IndexOf("@"));
                        if (nCount == 0 && nChkResults > 0)
                        {
                            strCSVtitle = results.Substring(results.IndexOf("@"), nChkResults);
                            strCSVtitle = strCSVtitle.Replace(Environment.NewLine, "");
                        }

                        switch (strSubCatValues[nCount])
                        {
                            case "service":
                            case "residential":
                                {
                                    Decibeles = m_DecLocal; //40;
                                    break;
                                }
                            case "motorway":
                            case "trunk":
                                {
                                    Decibeles = m_DecHway; //83;
                                    break;
                                }
                            case "secondary":
                            case "primary":
                                {
                                    Decibeles = m_DecPrimary; //65;
                                    break;
                                }
                            case "bus_stop":
                                {
                                    Decibeles = m_DecHBusStop; //85;
                                    break;
                                }
                            case "motorway_junction":
                                {
                                    Decibeles = m_DecHJunction; //85;
                                    break;
                                }
                            case "line":
                                {
                                    Decibeles = m_MainPowerLine; //50;
                                    break;
                                }
                            case "minor_line":
                                {
                                    Decibeles = m_MinorPowerLine; //50;
                                    break;
                                }
                        }
                        //let's extract and sort the data
                        if (array == null)
                            array = SortData(results, Decibeles, lon, lat, Adminpwd);
                        else
                        {
                            OsmData[] temparr = SortData(results, Decibeles, lon, lat, Adminpwd);
                            if (temparr != null && temparr.Length > 1)
                            {
                                OsmData[] Currentarr = array;
                                array = Currentarr.Concat(temparr).ToArray();
                            }
                        }

                        nCount++;
                    }

                    //let's calculate the value and convert the results to XML/string format
                    strContent = CalcFinalResults(lon, lat, array, Adminpwd, strCSVtitle);
                }
                else
                    strContent = ExtractOSMMapProdProd.Properties.Resources.InvalidData;
            }
            catch (Exception ex)
            {
                strContent = ex.Message;
            }

            m_Product.Add(strContent);
        }
        #endregion

        #region private functions

        private double distance(double lat1, double lon1, double lat2, double lon2, string unit)
        {
            //this function calculate the distance between 2 points with geographic coordinates

            //the distance calcukation between 2 point with same coordinates is 0
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                // all the claculated had been done/converted to Radians units
                var radlat1 = Math.PI * lat1 / 180;
                var radlat2 = Math.PI * lat2 / 180;
                var theta = lon1 - lon2;
                var radtheta = Math.PI * theta / 180;
                var dist = Math.Sin(radlat1) * Math.Sin(radlat2) + Math.Cos(radlat1) * Math.Cos(radlat2) * Math.Cos(radtheta);
                if (dist > 1)
                {
                    dist = 1;
                }
                dist = Math.Acos(dist);
                dist = dist * 180 / Math.PI;
                dist = dist * 60 * 1.1515;
                if (unit == "K")
                    dist = dist * 1.609344;
                if (unit == "N")
                    dist = dist * 0.8684;
                return dist;
            }
        }

        private void CalculateDistanceAndClassify(ref OsmData[] arr, double nLonOrg, double nLatOrg)
        {
            //let's classify and add the points styles to each point in the array
            //according to the distance from the center point (clicked point)

            //loop through all the points in the array, calculate the distance with the center point
            //and set the style according to the distance category
            for (int i = 0; i < arr.Length - 1; i++)
            {
                double dist = distance(nLatOrg, nLonOrg, arr[i].nLat, arr[i].nLon, "K");
                if (dist <= 0.5)
                    arr[i].Style = 0;
                if (dist > 0.5 && dist < 1)
                    arr[i].Style = 1;
                if (dist > 1)
                    arr[i].Style = 2;
            }
        }

        private void CalculateDistanceAndDecibleEqu(ref OsmData[] arr, double decible, double nLonOrg, double nLatOrg)
        {
            ///let's calculate the decible for each point:
            //N(x) = n - 3*log2(x/l), 
            //n is constant value of 70db noise for residential road and 80db for main roads
            //x is the distance in meters between the point and the center point.
            //l is constant of initial buffer distance from the road od 2 meter

            //loop through all the points in the array, calculate the decibles from the center point
            //to calculate the overall decible of the center point
            for (int i = 0; i < arr.Length; i++)
            {
                //calculate the distance between the points in km and convert the units to m
                double dist = distance(nLatOrg, nLonOrg, arr[i].nLat, arr[i].nLon, "K") * 1000;

                //N(x) = n - 3*log2(x/l)
                arr[i].Decibles = Math.Round((decible - 3 * Math.Log2(dist / 2)), 3);
            }
        }

        private double CalculateNewRange(double Decible)
        {
            double OldMax = m_MaxDecibels /*80*/, OldMin = m_MinDecibels;//45; 
            double NewMax = m_MaxRange /*10*/, NewMin = m_MinRange; //0
            //else if (Decible < m_MaxDecibels)
            //    return 0;

            double OldRange = (OldMax - OldMin);
            double NewRange = (NewMax - NewMin);
            double NewValue = NewMax - (((Decible - OldMin) * NewRange) / OldRange);

            //let's check for exceptional values to avoid range daviation
            if (Decible > m_MaxDecibels || NewValue > 10)
                return 10;
            else
                return NewValue;
        }

        private double CalculateDistanceAndDecibleEquAll(OsmData[] arr)
        {
            //let's calculate the overall decibles value through the equation
            //10x𝑙𝑜𝑔(10^(𝐿𝐼,1/10)+10^(𝐿𝐼,2/10)+10^(𝐿𝐼,3/10)+⋯+10^(𝐿𝐼,𝑛/10))
            //where L1,1 and L1,2...L1,n is the distance that had already been calculated in the
            //previous funcion: CalculateDistanceAndDecibleEqu

            //loop through all the points in the array to get the decible value and calculate the
            //overall decible of the center (choosen) point
            double sum = 0;
            for (int i = 0; i < arr.Length; i++)
                //calculate the distance between the points in km and convert the units to m
                sum += Math.Pow(10, arr[i].Decibles / 10);

            //calculate the 10* log for the sum value
            if (sum == 0)
                return 0;
            else
                return Math.Round(CalculateNewRange(10 * Math.Log10(sum)), 2);
            //return Math.Round(10 * Math.Log10(sum), 2);
        }

        private OsmData[] RemoveDuplicated(OsmData[] arr)
        {
            //let's remove close points from array according to the array sort and comparing between
            //following points in the array, in addition we will check for each new point the distance with
            //the prevoius points in the new array

            OsmData[] newArray = new OsmData[arr.Length];

            //we will pass 0,0 coordinates and consider it as empty point
            int i = 0, j = 0;
            while (arr[i].nLat == 0 && arr[i].nLon == 0)
                i++;
            newArray[j] = arr[i];
            while (i < arr.Length - 1)
            {

                //let's get the 1st point in the set and loop the following points to remove close points
                //and decrease the density of the points
                OsmData nPointValueOrg = arr[i];
                OsmData nPointValueNext = arr[i];
                while ((distance(nPointValueOrg.nLat, nPointValueOrg.nLon, nPointValueNext.nLat, nPointValueNext.nLon, "K") < 0.2) && (i < arr.Length - 1))
                {
                    i++;
                    nPointValueNext = arr[i];
                }
                newArray[j] = arr[i];

                nPointValueOrg = newArray[j];
                bool bExist = false; int k = j - 1;
                //let's check for close points before adding the new far point
                //this done because the quick sort is sorting 2 parameters in accuracy of ~70-80%
                //we still have cases where  far points from the 1st set point could be closed to other
                //already inserted points from previous sets
                while ((!bExist) && k >= 0)
                {
                    nPointValueNext = newArray[k];
                    bExist = (distance(nPointValueOrg.nLat, nPointValueOrg.nLon, nPointValueNext.nLat, nPointValueNext.nLon, "K") < 0.2);
                    k--;
                }

                if (!bExist)
                    j++;
            }

            //since we had a new smaller array after remove close points, let's resize the array accordingly
            Array.Resize<OsmData>(ref newArray, j);
            return newArray;
        }

        private string CalcFinalResults(double nLonOrg, double nLatOrg, OsmData[] array, string strAdminpwd, string strCSVtitle)
        {
            //initialaize the result string
            string result = string.Empty;

            //let's check if we have data to calculate in the selected area
            //if not we will put 10 - means that there is not streets and the selected location
            //is very quite
            if (array == null || array.Length == 1)
                return ("10.0");

            if (strAdminpwd == "AGER44221")
            {
                //let's classify the distances accroding to style groups
                //CalculateDistanceAndClassify (ref arrayNoDuplicates, nLonOrg, nLatOrg);
                CalculateDistanceAndClassify(ref array, nLonOrg, nLatOrg);

                //let's convert back the array to csv format
                //result = ConvertArrtoCsv(",", arrrayNoDuplicates);
                CsvConverter csvConverter = new CsvConverter();
                strCSVtitle += ",type,decibles";
                if (!m_IncludeTitle)
                    strCSVtitle = "";
                csvConverter.strCSVTitle = strCSVtitle;
                csvConverter.strSaperator = ",";
                result = csvConverter.ConvertArrtoCsv(array);
            }
            else
                //let's calculate the overall center point decibles value
                //result = Math.Round(CalculateDistanceAndDecibleEquAll(arrayNoDuplicates),3).ToString() + " Db";
                result = Math.Round(CalculateDistanceAndDecibleEquAll(array), 3).ToString();

            return (result);
        }
        private OsmData[] SortData(string strInputData, double decible, double nLonOrg, double nLatOrg, string strAdminpwd)
        {
            //let's convert the OSM string points output to array and sort it according to QuickSort methodology
            string[] strArray = strInputData.Split(Environment.NewLine.ToCharArray());
            strArray = strArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            OsmData[] array = new OsmData[strArray.Length];

            for (int i = 0; i < strArray.Length - 1; i++)
            {
                var values = strArray[i + 1].Split(',');
                //let's parse all the items from the string in each line
                //and store each value in the dedicated variable
                array[i].ItemId = Convert.ToInt64(values[0]);
                array[i].nLat = Convert.ToDouble(values[2]);
                array[i].nLon = Convert.ToDouble(values[1]);
                array[i].Category = Convert.ToString(values[3]);
                array[i].SubCategory = Convert.ToString(values[4]);
                array[i].Name = Convert.ToString(values[5]);
            }

            //let's run quick sort algorithm 
            //left side the strat of the array - right end of the array
            Quick_Sort(ref array, 0, array.Length - 1);

            //let's remove close points in the array to reduce point cloud intensity 
            //OsmData[] arrayNoDuplicates =  RemoveDuplicated(array);

            //let's calculate distances and decibles for each point
            //CalculateDistanceAndDecibleEqu(ref arrayNoDuplicates, nLonOrg, nLatOrg);
            CalculateDistanceAndDecibleEqu(ref array, decible, nLonOrg, nLatOrg);

            //return the data array (OSMdata format)
            return (array);
        }


        private string RunProcess(string strPath, string strExecfile, string strParam)
        {
            try
            {
                // run batch files using System Process method
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.RedirectStandardOutput = true;
                startInfo.WorkingDirectory = strPath;
                startInfo.FileName = strPath + System.IO.Path.DirectorySeparatorChar + strExecfile;
                startInfo.Arguments = strParam;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit(1000);
                string strOutput = "";
                int nCount = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    //get all the output received lines from the OSM data filter process
                    if (nCount > 1)
                        strOutput = strOutput + System.Environment.NewLine + line;
                    nCount++;
                }
                return strOutput;//process.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                return (ex.Message);
            }
        }

        private string BuildBoundryBox(double lon, double lat, double scale)
        {
            //if (scale < 0.004)
            //    scale = 0.004;
            //let's create a square boundy from the center coordinates to download the OSM data  
            double W = lon - 0.003; //* scale;
            double S = lat - 0.003; //* scale;
            double E = lon + 0.003; //* scale;
            double N = lat + 0.003; //* scale;
            return (W.ToString() + "," + S.ToString() + "," + E.ToString() + "," + N.ToString());
        }

        private bool OsmFileDownload(double lon, double lat, double scale)
        {
            //in this function we get the center coordinates (map clicked), create square boundry 
            //and download the OSM data file
            try
            {
                m_strFileName = Path.GetRandomFileName();
                WebClient webClient = new WebClient();
                webClient.DownloadFile(m_strOSMURL + BuildBoundryBox(lon, lat, scale),
                    m_strPath + System.IO.Path.DirectorySeparatorChar + m_strOutputFolder +
                    System.IO.Path.DirectorySeparatorChar + m_strFileName);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }


        }
        public override Product GetResult()
        {
            return m_Product;
        }
        #endregion
    }
}
