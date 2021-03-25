using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using static ExtractOSMMapProd.Controllers.ExtractOSMMapController;

namespace ExtractOSMMapProd
{
    public class ExtractOSMMap
    {
        #region ExtractOSMMap definition  
        #region propoerty members
        // osm extraction and filter configuration values
        private string m_strOutputFolder = "Data";
        private string m_strPath = @"C:\OSM\data\Converter";
        // SET PGPASSWORD=mypassword
        // osm2pgsql.exe -s -G -U postgres -d BirdEye -E 4326 -W --prefix test1 -W SampleData\map2.osm
        private string m_OSM2PGSQLExec = "osm2pgsql.bat";
        private string m_OSM2OGSQLPath = "osm2pgsql-bin";
        // -s, --slim Store temporary tables in database.This allows incremental updates using diff files also available at OSM data servers,
        // and reduces memory usage at a cost in disk space and import time.This mode of operation is recommended.
        // -G|--multi-geometry, Generate multi-geometry features in PostgreSQL tables. Normally osm2pgsql splits multi-part geometries 
        // into separate database rows per part. A single OSM id can therefore have several rows. With this option, PostgreSQL 
        // instead generates multi-geometry features in the PostgreSQL tables. Multi-geometry objects are a PostGIS feature 
        // representing collections of geometrical objects that can represent OSM relations combining multiple boundaries to an area (e.g. with holes).
        private string m_OSM2PGSQL_import_Flags = "-s -G";
        private string m_OSM2PGSQL_import_Crs = "-E 4326";
        private string m_OSM2PGSQL_import_Table_Prefix_Flag = "--prefix";
        // Database parameters 
        private string m_OSM2PGSQL_import_Database = "-d BirdEye";
        private string m_OSM2PGSQL_import_Database_Username = "-U postgres";
        //private string m_OSM2PGSQL_import_Database_Password = "-W";



        private Dictionary<string, string> dicOsmFiles;
        #endregion

        //url for downloading osm data as constant variable
        #region const variables
        const string m_strOSMExt = ".osm";
        const string m_strOSMURL = "https://api.openstreetmap.org/api/0.6/map?bbox=";
        const string m_strOSMURLbyCat = "http://www.overpass-api.de/api/xapi?*[bbox=][cat=*]";
        #endregion
        #endregion

        #region Public functions

        public ExtractOSMMap()
        {
            dicOsmFiles = new Dictionary<string, string>();
        }
        public string OsmFileDownload(double lon, double lat, double scale)
        
        {
            // In this function we get the center coordinates (map clicked), create square boundry 
            // and download the OSM data file

            string content = string.Empty;
          
            try
            {
                // let's add the character "a" to the generated random file name to make it compatible with PGSQL table names rules
                content = "a" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                WebClient webClient = new WebClient();
                // let's concatenate the osm file name full path
                string OSMFilePath = m_strPath + Path.DirectorySeparatorChar + m_strOutputFolder +
                    Path.DirectorySeparatorChar + content + m_strOSMExt;
                // let's download the file according to the coordinates bounding box
                double radius = 0.01;
                int downloadDataTries = 0;
                bool downloadSuccess = false;
                while (!downloadSuccess && downloadDataTries<5)
                {
                    try
                    {
                        webClient.DownloadFile(m_strOSMURL + BuildBoundryBox(lon, lat, radius, scale), OSMFilePath);
                        downloadSuccess = true;
                    }
                    catch
                    {
                        downloadDataTries += 1;
                        radius -= 0.001;
                    }
                }
                // let's insert the osm data from the OSM file to PGSQL tables using osm2pgsql 3rd party tool
                bool DataInserted = InsertOSMtoPGSQL(OSMFilePath, content);

                // let's check if the OSM file data was instered successfully to PGSQL, if not empty string will be returend
                if (!DataInserted)
                {
                    content = string.Empty;
                }
            }
            catch (Exception ex)
            {
                content = ex.Message;
            }
            return content;
        }
        #endregion

        #region Private functions
        private string BuildBoundryBox(double lon, double lat, double buffer, double scale)
        {
            //if (scale < 0.004)
            //    scale = 0.004;
            //let's create a square boundy from the center coordinates to download the OSM data  
            double W = lon - buffer; //* scale;
            double S = lat - buffer; //* scale;
            double E = lon + buffer; //* scale;
            double N = lat + buffer; //* scale;
            return (W.ToString() + "," + S.ToString() + "," + E.ToString() + "," + N.ToString());
        }


        private bool InsertOSMtoPGSQL (string OSMFilename, string tblPrefix)
        {
            try
            {
                string strProccessPath = m_strPath + Path.DirectorySeparatorChar + m_OSM2OGSQLPath;

                // another way to add arguments to the shell proccess but it didn't worked our in this case
                //string strOSMFileFullPath = m_strPath + Path.DirectorySeparatorChar + m_strOutputFolder + Path.DirectorySeparatorChar + OSMFilename;
                //List<string> lstParams = new List<string>();
                //lstParams.Add(m_OSM2PGSQL_import_Flags);
                //lstParams.Add(m_OSM2PGSQL_import_Crs);
                //lstParams.Add(m_OSM2PGSQL_import_Table_Prefix_Flag + " " + tblPrefix);
                //lstParams.Add(m_OSM2PGSQL_import_Database_Username);
                //lstParams.Add(m_OSM2PGSQL_import_Database);
                //lstParams.Add(OSMFilename);
                //RunProcess(strProccessPath, m_OSM2PGSQLExec, lstParams);

                // let's concatenate the osm2pgsql arguments as one string and run the shell proccess
                string strProccessParam = " " +  m_OSM2PGSQL_import_Flags + " " + m_OSM2PGSQL_import_Crs + " " + m_OSM2PGSQL_import_Table_Prefix_Flag +
                    " " + tblPrefix + " " + m_OSM2PGSQL_import_Database_Username + " " + m_OSM2PGSQL_import_Database + " " + OSMFilename;
                RunProcess(strProccessPath, m_OSM2PGSQLExec, strProccessParam);

                return (true);
            }
            catch
            {
                return (false);
            }
        }

        private string RunProcess(string strPath, string strExecfile, string strParam)
        {
            try
            {
                // run the batch file using System Process method
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.RedirectStandardOutput = true;
                // if the RedirectStandardError is not allowed in this case the process osm2pgsql.exe will be terminated before it's completed
                startInfo.RedirectStandardError = true;
                startInfo.WorkingDirectory = strPath;
                startInfo.FileName = strPath + Path.DirectorySeparatorChar + strExecfile;
                startInfo.Arguments = strParam;
                process.StartInfo = startInfo;
                process.Start();
                //string errOutputResult = process.StandardError.ReadToEnd();
                //string Outputresult = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return ("SUcess");
            }
            catch (Exception ex)
            {
                return (ex.Message);
            }
        }
        #endregion
    }
}
