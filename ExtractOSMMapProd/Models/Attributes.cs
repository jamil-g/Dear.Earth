using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExtractOSMMapProd.Models
{
    public class Attributes
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int DistanceFromEdge { get; set; }
        public int Ddb { get; set; }
        public int Concentration_1m { get; set; }
        public float EcologyDistanceFactor { get; set; }
        public float EcologyMark { get; set; }
        public float Factor { get; set; }
        public float Booster { get; set; }
    }
}
