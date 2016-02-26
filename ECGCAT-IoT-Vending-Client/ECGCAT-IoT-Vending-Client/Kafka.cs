using ECGCAT_IoT_Vending_Client;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Windows.Web.Http.Filters;

namespace ECGCAT_IoT_Vending_Client
{
    public class Kafka
    { 
        public async Task PostDataAsync(object _data, string _topic)
        {
            UriBuilder u1 = new UriBuilder();
            //u1.Host = "localhost"; //DEBUG
            u1.Host = "wssccatiot.westus.cloudapp.azure.com";
            u1.Port = 8082;
            u1.Path = "topics" + _topic;
            u1.Scheme = "http";
            Uri topicUri = u1.Uri;
            
            //Currently focused on REST API surface for Confluent.io Kafka deployment. We can make this more generic in the future
            string jsonBody = JsonConvert.SerializeObject(_data, Formatting.None);
            
            jsonBody = jsonBody.Replace(",", "}}, {\"value\":{"); //have to add in some json features into the string. Easier than creating unnecessary classes that would make this come out automatically
            string jsonHeader = ("{\"records\":[{\"value\":"); //same as above, fixing string for Server requirements
            string jsonFooter = ("}]}"); //ditto
            string json = jsonHeader + jsonBody + jsonFooter;

            var baseFilter = new HttpBaseProtocolFilter();
            baseFilter.AutomaticDecompression = false; //turn OFF header "Accept-Encoding"
            HttpClient httpClient = new HttpClient(baseFilter);
            try
            {
                var headerContent = new HttpStringContent(json);
                headerContent.Headers.ContentType = null; // removing all header content and will replace with the required values
                headerContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/vnd.kafka.json.v1+json"); //Content-Type: application/vnd.kafka.json.v1+json
                httpClient.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/vnd.kafka.json.v1+json, application/vnd.kafka+json, application/json")); //Add Accept: application/vnd.kafka.json.vl+json, application... header
                HttpResponseMessage postResponse = await httpClient.PostAsync(topicUri, headerContent);
            }
            catch
            {
                return;
            }

        }
    }
}