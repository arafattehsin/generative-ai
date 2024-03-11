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

namespace CustomCopilot.Plugins.PlaceSuggestionsPlugin
{
    public class PlaceSuggestionsPlugin
    {
        MapsSearchClient client;
        HttpClient httpClient = new HttpClient();
        string APIKey;

        public PlaceSuggestionsPlugin(string apiKey)
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
            var searchResult = await client.SearchAddressAsync(locationName);

            if (searchResult?.Value?.Results.Count() == 0) { return null; }

            SearchAddressResultItem locationDetails = searchResult!.Value.Results[0];

            string url = @$"https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&query={placeType}
                    &subscription-key={APIKey}
                    &lat={locationDetails.Position.Latitude}
                    &lon={locationDetails.Position.Longitude}
                    &countrySet=AU
                    &language=en-AU";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
    }
}
