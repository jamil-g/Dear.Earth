using Npgsql;
using System;
using System.Linq;
using System.Collections.Generic;
using static ExtractOSMMapProd.Controllers.ExtractOSMMapController;
using ExtractOSMMapProd.Models;

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
        //private readonly string Connectionstr = "Server=127.0.0.1;Port=5432;Database=BirdEye;User Id=postgres;Password=postgres;";
        private readonly string Connectionstr = "Server=18.132.162.121;Port=5432;Database=Dera;User Id=postgres;Password=koki_7yate32;";
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
            psqlconn.TypeMapper.MapComposite<Controllers.ExtractOSMMapController.Results>("result_all");
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

        public List<Controllers.ExtractOSMMapController.Results> CalculateIndcies(string tblPrefix, Coordinates coor, double baseValue, string userid, bool allowHistoryTrack, Types type, string EmailRecipients, string refno)
        {
            // this function calculate all/part of the attributes indcies by running the PGSQL calculation function
            List<Controllers.ExtractOSMMapController.Results> lstResults = null;
            using (var cmd = new NpgsqlCommand("select CalcIndices (@lon, @lat, @baseValue, @tblprefix, @userid, @EmailRecipients, @trackallow, @attribute, @refno);", psqlconn))
            {
                cmd.Parameters.AddWithValue("lon", coor.lon);
                cmd.Parameters.AddWithValue("lat", coor.lat);
                cmd.Parameters.AddWithValue("baseValue", baseValue);
                cmd.Parameters.AddWithValue("tblprefix", tblPrefix);
                cmd.Parameters.AddWithValue("userid", userid);
                cmd.Parameters.AddWithValue("EmailRecipients", EmailRecipients);
                cmd.Parameters.AddWithValue("trackallow", allowHistoryTrack);
                cmd.Parameters.AddWithValue("attribute", (int)type);
                cmd.Parameters.AddWithValue("refno", refno);

                // Execute the query and obtain a result set
                NpgsqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    lstResults = new List<Controllers.ExtractOSMMapController.Results>((Controllers.ExtractOSMMapController.Results[])dataReader[0]);
                }
                dataReader.Close();
                //List<Results> lstResultsNew = lstResults.Select(c => { c.type = (Types)(c.category-1); return c; }).ToList();
            }

            // let's update the attributes calculation according to the enum using Linq syntax
            return lstResults.Select(c => { c.type = (Types)c.category; return c; }).ToList();
        }

        public int LoadCategory(string type)
        {
            int catCode = -1;
            using (var cmd = new NpgsqlCommand("SELECT \"Code\" FROM \"MainLayersTbl\" where \"Value\"=(@CatType)", psqlconn))
            {
                cmd.Parameters.AddWithValue("CatType", type);
                // Execute the query and obtain a result set
                NpgsqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    catCode = Convert.ToInt32(dr[0]);
                }
                dr.Close();
            }
            return catCode;
        }

        public List<Attributes> LoadAttributes(int catType)
        {
            List<Attributes> attrLst = new List<Attributes>();
            Attributes item;
            using (var cmd = new NpgsqlCommand("SELECT \"category\", \"subcategory\", \"distancefromedge\", \"ddb\", \"concentration_1m\", \"decnum\", \"ecologymark\", \"factor\", \"booster\" FROM \"attributesparamtbl\" where \"cattype\"=(@CatType)", psqlconn))
            {
                cmd.Parameters.AddWithValue("Cattype", catType);
                // Execute the query and obtain a result set
                NpgsqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    item = new Attributes();
                    item.Category = Convert.ToString(dr[0]);
                    item.SubCategory = Convert.ToString(dr[1]);
                    item.DistanceFromEdge = Convert.ToInt16(dr[2]);
                    item.Ddb = Convert.ToInt16(dr[3]);
                    item.Concentration_1m = Convert.ToInt16(dr[4]);
                    item.EcologyDistanceFactor = Convert.ToInt16(dr[5]);
                    item.EcologyMark = Convert.ToSingle(dr[6]);
                    item.Factor = Convert.ToSingle(dr[7]);
                    item.Booster = Convert.ToSingle(dr[8]);
                    attrLst.Add(item);
                }
                dr.Close();
            }
            return attrLst;
        }

        public List<CalcResults> Getdata(string tblName, Attributes itemGet, double BGRank, Coordinates coor)
        {
            List<CalcResults> lstResults = null;
            CalcResults itemSet;
            try
            {
                using (var cmd = new NpgsqlCommand($"SELECT ST_DistanceSphere(way, ST_SetSRID(ST_Point(@lon,@lat), 4326)) as Distance, ST_AsText(ST_SetSRID(st_centroid(way), 4326)) as geom FROM {tblName} where {itemGet.Category} = @SubCat", psqlconn))
                {
                    cmd.Parameters.AddWithValue("lon", coor.lon);
                    cmd.Parameters.AddWithValue("lat", coor.lat);
                    //cmd.Parameters.AddWithValue("Cat", itemGet.Category);
                    cmd.Parameters.AddWithValue("SubCat", itemGet.SubCategory);
                    NpgsqlDataReader dr = cmd.ExecuteReader();
                    lstResults = new List<CalcResults>();
                    while (dr.Read())
                    {
                        itemSet = new CalcResults();
                        itemSet.Distance = Convert.ToSingle(dr[0]);
                        string point = Convert.ToString(dr[1]);
                        // let's extract from the point string the lon and lat values, expected format: POINT(35.218108215227 31.7562651480025)
                        point = point.Replace("POINT(", "").Replace(")", "");
                        string[] points = point.Split(" ");
                        itemSet.Lon = Convert.ToDouble(points[0]);
                        itemSet.Lat = Convert.ToDouble(points[1]);
                        itemSet.BGRank = BGRank;
                        itemSet.Category = itemGet.Category; itemSet.SubCategory = itemGet.SubCategory;
                        itemSet.SourceLon = coor.lon; itemSet.SourceLat = coor.lat;
                        lstResults.Add(itemSet);
                    }
                    dr.Close();
                }
            }
            catch (Exception ex)
            {

            }
            return lstResults;
            // SELECT ST_AsText(ST_SetSRID(st_centroid(way),4326)) as geom FROM a20210919_062551_line
            //https://postgis.net/docs/ST_Centroid.html
        }
        #endregion
    }
}
