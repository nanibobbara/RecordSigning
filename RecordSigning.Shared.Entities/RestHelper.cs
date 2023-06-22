using Azure.Core.GeoJson;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecordSigning.Shared
{
    public static class RestHelper
    {
        public static async Task<KeyRing> GetResponse(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                while (true)
                {
                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the request was successful (status code 200)
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string content = await response.Content.ReadAsStringAsync();
                        KeyRing key = JsonSerializer.Deserialize<KeyRing>(content);

                        if (key != null)
                        {
                            return key;
                        }
                    }

                    // Wait for a short period before making the next request
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

            }
        }

        public static async Task PutResponse(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                // Make the PUT request
                await client.PutAsync(url,null);
                
            }
        }
    }
}
