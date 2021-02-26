using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ExtractOSMMapProd
{
    public class Director
    {
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double Scale { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set;}
        public string Adminpwd { get; set; }

        // The method run the calculation of different noise/radiation calculation in a sequence order
        // and printout the results.
        public string CalculateAllFactores()
        {
            string Result;
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddEventLog();
            });
            ILogger<NoiseInterference> logger = loggerFactory.CreateLogger<NoiseInterference>();

            NoiseInterference builder = new NoiseInterference(logger);
            builder.CalculateLocationQuality(Lon, Lat, Scale, Category, SubCategory, Adminpwd);
            Result = builder.GetResult().ReturnResults();

            // to add the ground quality calculation 
            return (Result);
        }
    }

}
