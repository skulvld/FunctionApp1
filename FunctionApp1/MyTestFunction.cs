using System;  
using System.IO;
using System.Threading.Tasks;  
using Microsoft.AspNetCore.Mvc;  
using Microsoft.Azure.WebJobs;  
using Microsoft.Azure.WebJobs.Extensions.Http;  
using Microsoft.AspNetCore.Http;  
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;  
using Microsoft.Azure.Storage;  
using Microsoft.Azure.Storage.Blob;
using System.Net.Http;

namespace FunctionApp1
{
    public static class MyTestFunction
    {
        [FunctionName("MyTestFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Http trigger function executed at: {DateTime.Now}");

            //Creting container
            CreateContainerIfNotExists(context);

            //Get container where will be saved files
            CloudStorageAccount storageAccount = GetCloudStorageAccount(context);
            CloudBlobClient     blobClient     = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer  container      = blobClient.GetContainerReference("test-messages-folder");

            //Set name
            string randomStr = Guid.NewGuid().ToString() + ".json";
            CloudBlockBlob blob = container.GetBlockBlobReference(randomStr);
            blob.Properties.ContentType = "application/json";

            //HTTP request / response
            var client = new HttpClient();
            var url = "https://api.sampleapis.com/coffee/hot";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();

            //Getting JsonObj
            var deserializedJsonObj = JsonConvert.DeserializeObject(resp);

            //Store object in memory
            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, deserializedJsonObj);
                await blob.UploadFromStreamAsync(ms);
            }
            log.LogInformation($"Bolb {randomStr} is uploaded to container {container.Name}");
            await blob.SetPropertiesAsync();

            return new OkObjectResult("MyTestFunction executed successfully!");
        }

        /// <summary>
        /// Creates contaner if it wasn't created earlier.
        /// </summary>
        /// <param name="executionContext">ExecutionContext</param>
        private static void CreateContainerIfNotExists(ExecutionContext executionContext)
        {
            CloudStorageAccount storageAccount = GetCloudStorageAccount(executionContext);
            CloudBlobClient     blobClient     = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer blobContainer = blobClient.GetContainerReference("test-messages-folder");
            blobContainer.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Connection to storage account.
        /// </summary>
        /// <param name="executionContext">ExecutionContext</param>
        /// <returns>CloudStorageAccount</returns>
        private static CloudStorageAccount GetCloudStorageAccount(ExecutionContext executionContext)
        {
            string accountName      = "teststrgvld";
            string accessKey        = "wBLksW2hV0OcXWN+2TFizPLQFBOLqLZxF5aKTIgOCmRyojQak1TAyq9Giz1trhfvUiylMaksk3F/SNT1gVGFyQ==";
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accessKey + ";EndpointSuffix=core.windows.net";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            return storageAccount;
        }

        /// <summary>
        /// Writes data to the stream.
        /// </summary>
        /// <param name="ms">MemoryStream</param>
        /// <param name="obj">object</param>
        private static void LoadStreamWithJson(Stream ms, object obj)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(obj); //write
            writer.Flush();    //clear
            ms.Position = 0;
        }
    }
}
