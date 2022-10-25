using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Model;
using PureCloudPlatform.Client.V2.Extensions;

namespace Genesys
{

    public class PreRoutingData{
        public string CLI {get;set;}
        public string Destination {get; set;}
    }

    class Program
    {

        private static string clientId = "";
        private static string clientSecret = "";

        static void Main(string[] args)
        {
            PureCloudRegionHosts region = PureCloudRegionHosts.eu_west_2; // Genesys Cloud region
            Configuration.Default.ApiClient.setBasePath(region);
            
            // Configure OAuth2 access token for authorization: PureCloud OAuth
            // The following example is using the Client Credentials Grant
            var accessTokenInfo = Configuration.Default.ApiClient.PostToken(clientId, clientSecret);

            //read our data from a database - I'll just use a List of strings to simlulate this.
            List<PreRoutingData> dbdata = new List<PreRoutingData>();
            dbdata.Add(new PreRoutingData(){ CLI="447717123456", Destination= "Fraud"});
            dbdata.Add(new PreRoutingData(){ CLI="447717789101", Destination= "Fraud"});
            dbdata.Add(new PreRoutingData(){ CLI="447717123101", Destination= "Collections"});

            //convert the data into a CSV - we need to include the headers.  HOWEVER the CLI is used as our
            //KEY and therefore we call that field KEY not CLI. 
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("key,routingoverride");
            foreach(var route in dbdata){
                sb.AppendLine(route.CLI + "," + route.Destination);
            }
            string data = sb.ToString();


            //firts job is to create an Import Job - this will return and importUri we need to upload our actual data to.
            string importUri = CreateImportJob(accessTokenInfo);

            //once we have the URU we need to POST the data up as a file.
            if(importUri!=null){
                UploadFile("clidata.csv", importUri, accessTokenInfo.AccessToken, data);
            }
        }

        public static string CreateImportJob(AuthTokenInfo authToken){
            

            var apiInstance = new ArchitectApi();
            var datatableId = "9b72f2e3-281a-40e8-a78d-c509fdebeaec";  // string | id of datatable
            var body = new DataTableImportJob(); // DataTableImportJob | import job information
            body.ImportMode=DataTableImportJob.ImportModeEnum.Replaceall;

            try
            { 
                // Begin an import process for importing rows into a datatable
                DataTableImportJob result = apiInstance.PostFlowsDatatableImportJobs(datatableId, body);
                Console.WriteLine(result);
                return result.UploadURI;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when calling Architect.PostFlowsDatatableImportJobs: " + e.Message );
                return null;
            }

            

        }

        public static string UploadFile(string fileName, string UriUploadPath, string bearerToken, string data)
        {

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using (var multipartFormContent = new MultipartFormDataContent())
            {

                //Load the file and set the file's Content-Type header
                var fileStreamContent = new StreamContent(new MemoryStream(Encoding.ASCII.GetBytes(data)));
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

                //Add the file
                multipartFormContent.Add(fileStreamContent, name: "file", fileName: fileName);

                //Send it
                var response = httpClient.PostAsync(UriUploadPath, multipartFormContent).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }

        }


        
        //we don't need this method - but I'll leave it here for referemce in case we have issues authenticating.
        public static string GetToken()
        {
            var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            });
            var basicAuth = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(clientId + ":" + clientSecret));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            var response = httpClient.PostAsync("https://login.euw2.pure.cloud/oauth/token", content).Result;
            var token = JObject.Parse(response.Content.ReadAsStringAsync().Result)["access_token"].ToString();
            return token;
        }
    }


}