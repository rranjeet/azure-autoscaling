#r "Microsoft.ServiceBus"

using System.Net;

using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

public class PanMessage<T>
{
    public string version {get; set;}
    public string status {get; set;}
    public string operation {get; set;}
    public T context {get; set;}
}

public class ScaleMessageContext 
{
    public string timestamp {get; set;}
    public string id {get; set;}
    public string name {get; set;}
    public string details {get; set;}
    public string subscriptionId {get; set;}
    public string resourceGroupName {get; set;}
    public string resourceName {get; set;}
    public string resourceType {get; set;}
    public string resourceId {get; set;}
    public string portalLink {get; set;}
    public string oldCapacity {get; set;}
    public string newCapacity {get; set;}

    public override string ToString()
    {
        string ctxt = "";
        ctxt += "TS:\t" + this.timestamp + "\n";
        ctxt += "ID:\t" + this.id + "\n";
        ctxt += "Name:\t" + this.name + "\n";
        ctxt += "Details:\t" + this.details + "\n";
        ctxt += "SubsId:\t" + this.subscriptionId + "\n";
        ctxt += "RGName:\t" + this.resourceGroupName + "\n";
        ctxt += "ResName:\t" + this.resourceName + "\n";
        ctxt += "ResType:\t" + this.resourceType + "\n";
        ctxt += "ResId:\t" + this.resourceId + "\n";
        ctxt += "PorLnk:\t" + this.portalLink + "\n";
        ctxt += "OldCap:\t" + this.oldCapacity + "\n";
        ctxt += "NewCap:\t" + this.newCapacity + "\n";

        return ctxt;
    }
}


// Service Bus end point is read as an environment variable
// For example - 
// Endpoint=sb://pa-vm-autoscaling-servicebus7.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=VdBYi0jWRTjcKOhyg05x3/BPj7rlZYfg8xSe1o/yjlA= 
static async Task<string> SendMessage(string ConnectionString, string QueueName, string msg, TraceWriter log)
{
     Microsoft.Azure.ServiceBus.Message sbMsg = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(msg));
     IQueueClient qClient = null;
     try
     {
         qClient = new QueueClient(ConnectionString, QueueName);
         await qClient.SendAsync(sbMsg);
     }
     catch (System.Exception e)
     {
         log.Info(e.ToString());
     }
     
     await qClient.CloseAsync();
     return "Ok";
}

public static HttpResponseMessage Run(HttpRequestMessage request, TraceWriter log)
{
    string[] strServiceBusDelimiters = {";"};
    string request_body = request.Content.ReadAsStringAsync().Result;
    log.Info("Received a HTTP Request " + request_body);

    string http_method = request.Method.ToString();

    if (http_method == "GET")
    {
        string instrumentationKey = request.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "ik", true) == 0)
        .Value;
        log.Info("Instrumentation key " + instrumentationKey);

        var template = @"{'$schema': 'https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#', 'contentVersion': '1.0.0.0', 'parameters': {}, 'variables': {}, 'resources': []}";
        HttpResponseMessage myResponse = request.CreateResponse(HttpStatusCode.OK);
        myResponse.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/json");
        return myResponse;
    }

    if (http_method == "POST")
    {
        string [] req_lines = request_body.Split('\n');
        foreach(string line in req_lines)
        {
            if (line.ToLower().Contains("operation"))
            {
                log.Info(line);
            }
        }

        PanMessage<ScaleMessageContext> msg = JsonConvert.DeserializeObject<PanMessage<ScaleMessageContext>>(request_body);
        log.Info(msg.context.ToString());

        var ServiceBusConnectionString =  Environment.GetEnvironmentVariable("PanServiceBusConnectionString", EnvironmentVariableTarget.Process);
        string QueueName = msg.context.subscriptionId;
        string result = SendMessage(ServiceBusConnectionString, QueueName, request_body, log).GetAwaiter().GetResult();
        return new HttpResponseMessage( HttpStatusCode.OK ) {Content = new StringContent(result)};
    }

    return new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent("Invalid request")};
}

