using Azure.Maps.Search.Models;
using Azure.Maps.Search;
using Azure;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CustomCopilot.Plugins.PlaceSuggestions
{
    public class PlaceSuggestions
    {
        MapsSearchClient client;
        HttpClient httpClient = new HttpClient();
        string APIKey;

        public PlaceSuggestions(string apiKey)
        {
            APIKey = apiKey;
            AzureKeyCredential credential = new(apiKey);
            client = new MapsSearchClient(credential);
        }

        [KernelFunction, Description("Gets the place suggestions for a given location")]
        [return: Description("Place suggestions")]
        public async Task<string> GetPlaceSuggestionsAsync(
        [Description("type of the place")] string placeType,
        [Description("name of the location")] string locationName)
        {
            var searchResult = await client.GetGeocodingAsync(locationName);

            if (searchResult?.Value.Features.Count() == 0) { return null; }

            FeaturesItem locationDetails = searchResult!.Value.Features.First();

            string url = @$"https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&query={placeType}&subscription-key={APIKey}&lat={locationDetails.Geometry.Coordinates.Latitude}&lon={locationDetails.Geometry.Coordinates.Longitude}&countrySet=AU&language=en-AU";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
    }
}
