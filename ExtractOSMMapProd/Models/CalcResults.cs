using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExtractOSMMapProd.Models
{
    public class CalcResults
    {
        [JsonProperty("Category")]
        public String Category { get; set; }

        [JsonProperty("SubCategory")]
        public String SubCategory { get; set; }

        [JsonProperty("SourceLon")]
        public double SourceLon { get; set; }

        [JsonProperty("SourceLat")]
        public double SourceLat { get; set; }

        [JsonProperty("BGRank")]
        public double BGRank { get; set; }

        [JsonProperty("Lon")]
        public double Lon { get; set; }

        [JsonProperty("Lat")]
        public double Lat { get; set; }

        [JsonProperty("Distance")]
        public double Distance { get; set; }

    }
}
