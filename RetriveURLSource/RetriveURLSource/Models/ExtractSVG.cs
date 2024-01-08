using System;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;

namespace RetriveURLSource.Models
{
    public class ExtractSVG
    {
        public string ExtractSVGAsStr(string url) {
            try
            {
                new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
                IWebDriver driver = new ChromeDriver();
                driver.Manage().Window.Maximize();
                driver.Navigate().GoToUrl(url);
                //driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);
                //Thread.Sleep(new TimeSpan(0, 0, 3));
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 5000) ;
                //{
                // Console.WriteLine("Waiting for page to load");
                //}
                string pageSource = driver.PageSource;
                int start = pageSource.IndexOf("<svg width=\"350\"");
                int end = pageSource.IndexOf("/svg>") + 5;
                string svg = pageSource.Substring(start, end - start);
                //Console.WriteLine(svg);
                driver.Close();
                driver.Quit();
                return (svg);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
