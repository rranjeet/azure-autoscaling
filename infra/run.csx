#r "Microsoft.ServiceBus"

using System.Net;

using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

public class Message 
{
    public string version {get; set;}
    public string status {get; set;}
    public string operation {get; set;}
    public ScaleMessageContext context {get; set;}
    
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

static async Task SendMessage(string ConnectionString, string QueueName, string msg)
{
     Microsoft.Azure.ServiceBus.Message sbMsg = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(msg));
     IQueueClient qClient = new QueueClient(ConnectionString, QueueName);
     await qClient.SendAsync(sbMsg);
     await qClient.CloseAsync();
}

public static HttpResponseMessage Run(HttpRequestMessage request, TraceWriter log)
{
    string[] strServiceBusDelimiters = {";"};
    log.Info("Received a HTTP Request.");

    string request_body = request.Content.ReadAsStringAsync().Result;

    Message msg = JsonConvert.DeserializeObject<Message>(request_body);
    log.Info(msg.context.ToString());
    var ServiceBusConnectionString =  Environment.GetEnvironmentVariable("PanServiceBusConnectionString", EnvironmentVariableTarget.Process);
    string QueueName = msg.context.subscriptionId;

    SendMessage(ServiceBusConnectionString, QueueName, request_body).GetAwaiter().GetResult();
    
    return new HttpResponseMessage( HttpStatusCode.OK ) {Content =  new StringContent( "Your message here" ) };
    
}

