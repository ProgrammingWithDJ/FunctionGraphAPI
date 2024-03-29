using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace FunctionGraphAPI
{
    public static class Function1
    {
        [FunctionName("GraphNotificationHook")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var config = new ConfigurationBuilder().AddEnvironmentVariables()
                .Build();

            // parse query parameter
            var validationToken = req.Query["validationToken"];

            var secret = config["MySecret"]; // Stored the secret in the keyvault

            log.LogInformation(validationToken);
            if (!string.IsNullOrEmpty(validationToken))
            {
               
                log.LogInformation("validationToken: " + validationToken);
                log.LogInformation("Sending validation token");
               
                return new ContentResult { Content = validationToken, ContentType = "text/plain" };
            }

            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GraphNotification>(requestBody);
            foreach (var notification in data.value)
            {
                if (notification.Resource.Any())
                {
                    log.LogInformation($"Recived Notification : '{notification.Resource}', {notification.Id}");
                    log.LogInformation("Change Type" + notification.ChangeType.Value.ToString());
                }

                if(notification.LifecycleEvent.HasValue)
                {
                    log.LogInformation($"Missed notification Alert: '{notification.LifecycleEvent}', {notification.SubscriptionExpirationDateTime}");
                    log.LogInformation("Missed Type" + notification.LifecycleEvent.Value.ToString());
                }
            }

            if (!data.value.FirstOrDefault().ClientState.Equals(secret, StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("client state not valid");
                //client state is not valid (doesn't much the one submitted with the subscription)
                return new BadRequestResult();
            }
            //do something with the notification data
            log.LogInformation("sending 200 ok");
            return new OkResult();
        }
    }
}
