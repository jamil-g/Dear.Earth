using System;
using log4net;
using System.IO;
using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace OSM.ExportMapImage
{
    public class ExportMapImage : IExportMapImage
    {
        #region properties and members definition
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private static readonly string exportpath = @"C:\OSM\data\MapExport\Data";
        #endregion

        #region public methods
        public string ExportImage(double lon, double lat, int zoom)
        {
            try
            {
                string exportpath = @"C:\OSM\data\MapExport\Data";
                string filename = $@"{Guid.NewGuid()}" + ".png";
                string exporturl = $"https://www.ager.earth/OSM/ExportMap/index.html?lat={lat}&lon={lon}&zoom={zoom}&filename=" + filename + "&format=png";

                //FirefoxOptions fxProfile = new FirefoxOptions();

                //fxProfile.SetPreference("browser.download.folderList", 2);
                //fxProfile.SetPreference("browser.download.manager.showWhenStarting", false);
                //fxProfile.SetPreference("browser.download.dir", exportpath);
                ////fxProfile.SetPreference("browser.helperApps.alwaysAsk.force", false);
                //fxProfile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "image/png,image/jpeg");
                //IWebDriver driver = new FirefoxDriver(fxProfile);
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddUserProfilePreference("download.default_directory", exportpath);
                chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
                chromeOptions.AddUserProfilePreference("disable-popup-blocking", "true");
                //var driver = new ChromeDriver(@"C:\OSM\BrowserDriver\GoogleChrome", chromeOptions);

                Uri uri = new Uri("http://localhost:9515");
                var driver = new RemoteWebDriver(uri, chromeOptions);
                driver.Navigate().GoToUrl(exporturl);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(40);
                IWebElement element = driver.FindElement(By.Id("Done"));

                driver.Close();
                driver.Quit();
                driver.Dispose();

                string mapLayoutImage = exportpath + Path.DirectorySeparatorChar + filename;
                Image backImg = Image.FromFile(mapLayoutImage);
                //Image NorthImg = Resource.NorthArrow_small; //Image.FromFile("../../images/NorthArrow_small.png");
                Image ScaleImg = Resource.scalebar; //Image.FromFile("../../images/scalebar.png");
                Image mrkImg = Resource.Navigator; //Image.FromFile("../../images/AGER.png");
                Graphics g = Graphics.FromImage(backImg);
                //g.DrawImage(NorthImg, 0, 0);
                g.DrawImage(mrkImg, (backImg.Width / 2) - 23, (backImg.Height / 2) - 104);
                g.DrawImage(ScaleImg, backImg.Width - 330, backImg.Height - 40);
                string mapLayoutImageNew = Path.ChangeExtension(mapLayoutImage, null);
                mapLayoutImageNew += "_new.png";
                backImg.Save(mapLayoutImageNew);
                return mapLayoutImageNew;
            }
            catch (Exception e)
            {
                log.Error(e);
                return string.Empty;
            }
        }
        #endregion
    }
}
