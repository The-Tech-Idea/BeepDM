using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DemoLibrary
{
    public static class ApiHelper
    {
        public static HttpClient ApiClient { get; set; }
        public static string RequestUrl { get; set; } 
        public static void InitializeClient()
        {
            ApiClient = new HttpClient();
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public static async Task<T> LoadAPIData<T>(string pRequestUrl,int comicNumber = 0)
        {

            RequestUrl = pRequestUrl;


            using (HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(RequestUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    dynamic retval = await response.Content.ReadAsStreamAsync();

                    return retval;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
}
