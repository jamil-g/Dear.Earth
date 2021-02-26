using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSM.ExportMapImage
{
    public interface IExportMapImage
    { 
        string ExportImage(double lat, double lon, int zoom);
    }
}
