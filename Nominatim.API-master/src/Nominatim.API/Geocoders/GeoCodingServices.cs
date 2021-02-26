using Nominatim.API.Models;


namespace Nominatim.API.Geocoders
{
    public class GeoCodingServices
    {
        #region Geocoding definition  
        #region propoerty members

        #endregion

        #endregion


        #region public methods definition  

        public AddressResult ReverseGeoCoding(double lon, double lat)
        {
            var reverseGeocode = new ReverseGeocoder();

            var result = reverseGeocode.ReverseGeocode(new ReverseGeocodeRequest
            {
                Longitude = lon,
                Latitude = lat,

                BreakdownAddressElements = true,
                ShowExtraTags = true,
                ShowAlternativeNames = true,
                ShowGeoJSON = true
            });
            result.Wait();
            string addressResult = string.Empty;
            if ((result.Result.Address != null))
            {
                return result.Result.Address;
            }
            return null;
        }

        public int GetCityRank(double lon, double lat)
        {
            var reverseGeocode = new ReverseGeocoder();

            var result = reverseGeocode.ReverseGeocode(new ReverseGeocodeRequest
            {
                Longitude = lon,
                Latitude = lat,

                BreakdownAddressElements = true,
                ShowExtraTags = true,
                ShowAlternativeNames = true,
                ShowGeoJSON = true
            });
            result.Wait();
            string addressResult = string.Empty;
            if ((result.Result.Address != null))
            {

                int index = string.IsNullOrEmpty(result.Result.Address.City) ? 0 : 1;
                if (index==0)
                    index = string.IsNullOrEmpty(result.Result.Address.Neighborhood) ? 0 : 1;
                if (index == 0)
                    index = string.IsNullOrEmpty(result.Result.Address.Town) ? 0 : 2;
                if (index == 0)
                    index = string.IsNullOrEmpty(result.Result.Address.Suburb) ? 0 : 3;
                if (index == 0)
                    index = string.IsNullOrEmpty(result.Result.Address.Village) ? 0 : 4;
                if (index == 0)
                    index = string.IsNullOrEmpty(result.Result.Address.Hamlet) ? 0 : 5;
                return index;
            }
            return 0;
        }
        #endregion
    }
}
