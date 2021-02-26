using System;
using log4net;
using System.IO;
using System.Linq;
using System.Drawing;
using OpenQA.Selenium;
using System.ComponentModel;
using OpenQA.Selenium.Firefox;


namespace OSM.ExportMapImage
{
    class Program
    {
        #region properties and members definition
        #endregion

        #region public methods        
        static void Main(string[] args)
        {
            ExportMapImage exportmap = new ExportMapImage();
            exportmap.ExportImage(32.80,34.977,15);
        }
        #endregion
    }
}
