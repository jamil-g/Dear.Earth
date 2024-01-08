
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using RetriveURLSource.Models;
using System;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;
using static System.Net.Mime.MediaTypeNames;

namespace RetriveURL
{
    class Runner
    {
        static void Main(string[] args)
        {
            ExtractSVG svg = new ExtractSVG();
            string Url = "https://www.dera.earth/osm/LULC/#15/8.69894/49.39944/0/";
            string svgStr = svg.ExtractSVGAsStr(Url);
            Console.WriteLine(svgStr); 
        }

    }
}
