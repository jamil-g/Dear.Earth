using Npgsql;
using System;
using System.Linq;
using System.Collections.Generic;
using static ExtractOSMMapProd.Controllers.ExtractOSMMapController;

namespace ExtractOSMMapProd
{
    public class SQLDataClass
    {
        #region properties members definition

        public NpgsqlConnection psqlconn;

        public struct ResutlsFormat
        {
            public long category;
            public double indexvalue;
        }

        #region consts members definition
        private readonly string Connectionstr = "Server=127.0.0.1;Port=5432;Database=BirdEye;User Id=postgres;Password=postgres;";
        #endregion

        #endregion

        #region public methods
        public SQLDataClass(string connectionstr)
        {
            psqlconn = new NpgsqlConnection();
            if (string.IsNullOrEmpty(connectionstr))
            {
                psqlconn.ConnectionString = Connectionstr;
            }
            else
            {
                psqlconn.ConnectionString = connectionstr;
            }
            psqlconn.Open();
            // we need to reference (connect) the same type/struct at the project and PGSQL
            // the properties should have exactly same name (case sensetive) and type 
            psqlconn.TypeMapper.MapComposite<Results>("result_all");
        }

        public string DeleteTables (string tblPrefix)
        {
            // this function drop all the tables of the same OSM file data 
            // to be used after the finalizing the calculation proccess
            string results = string.Empty;
            List<string> lstSuffix = new List<string>();
            lstSuffix.Add("_line");
            lstSuffix.Add("_nodes");
            lstSuffix.Add("_point");
            lstSuffix.Add("_polygon");
            lstSuffix.Add("_rels");
            lstSuffix.Add("_roads");
            lstSuffix.Add("_ways");

            foreach (string item in lstSuffix)
            {
                string tblName = tblPrefix + item;
                try
                {   
                    using (var cmd = new NpgsqlCommand("DROP TABLE " + tblName, psqlconn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    results += ex.Message + Environment.NewLine  + "Failed to drop table:" + tblName + Environment.NewLine;
                }
            }
            return results;
        }

        public List<Results> CalculateIndcies(string tblPrefix, Coordinates coor, double baseValue, string userid, bool allowHistoryTrack, Types type)
        {
            // this function calculate all/part of the attributes indcies by running the PGSQL calculation function
            List<Results> lstResults = null;
            using (var cmd = new NpgsqlCommand("select CalcIndices (@lon, @lat, @baseValue, @tblprefix, @userid, @trackallow, @attribute);", psqlconn))
            {
                cmd.Parameters.AddWithValue("lon", coor.lon);
                cmd.Parameters.AddWithValue("lat", coor.lat);
                cmd.Parameters.AddWithValue("baseValue", baseValue);
                cmd.Parameters.AddWithValue("tblprefix", tblPrefix);
                cmd.Parameters.AddWithValue("userid", userid);
                cmd.Parameters.AddWithValue("trackallow", allowHistoryTrack);
                cmd.Parameters.AddWithValue("attribute", (int)type);

                // Execute the query and obtain a result set
                NpgsqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    lstResults = new List<Results>((Results[])dataReader[0]);
                }
                dataReader.Close();
                //List<Results> lstResultsNew = lstResults.Select(c => { c.type = (Types)(c.category-1); return c; }).ToList();
            }

            // let's update the attributes calculation according to the enum using Linq syntax
            return lstResults.Select(c => { c.type = (Types)c.category; return c; }).ToList();
        }
        #endregion
    }
}
