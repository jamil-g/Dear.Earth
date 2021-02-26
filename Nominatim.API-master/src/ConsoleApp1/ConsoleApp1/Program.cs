using System;
using Nominatim.API.Geocoders;
using Nominatim.API.Models;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var y = new ReverseGeocoder();

            var r2 = y.ReverseGeocode(new ReverseGeocodeRequest
            {
                Longitude = 34.99799765244287,
                Latitude = 32.81943571434325,

                BreakdownAddressElements = true,
                ShowExtraTags = true,
                ShowAlternativeNames = true,
                ShowGeoJSON = true
            });
            r2.Wait();
            if ((r2.Result.Address!=null))
            {
                Console.WriteLine(r2.Result.Address.City);
                Console.ReadLine();
            }
        }
    }
}
