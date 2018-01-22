#r "Newtonsoft.Json"

using System;
using System.Environment;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    log.Info(GetEnvironmentVariable("TEXT_ANALYTICS_API_KEY", EnvironmentVariableTarget.Process));
    
    log.Info($"WebHook was triggered! Comment: '{data.comment.body}' on '{data.comment.title}'");

    return req.CreateResponse(HttpStatusCode.OK, new {
        body = $"New GitHub comment:  '{data.comment.body}' on '{data.comment.title}'"
    });
}
