#r "Newtonsoft.Json"

using System.Net;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    var sentimentScore = await AnalyzeSentiment(data.comment.body);

    string sentiment = "neutral";
//    if (sentimentScore <= 0.2) {
//       sentiment = "negative";
//    }
//    if (sentimentScore >= 0.8) {
//      sentiment = "positive";
//    }

    log.Info($"Sentiment was '{sentiment}'. Comment: '{data.comment.body}' on '{data.comment.title}'");

    return req.CreateResponse(HttpStatusCode.OK, new {
        body = $"New GitHub comment:  '{data.comment.body}' on '{data.comment.title}'"
    });
}

static async Task<double> AnalyzeSentiment(string comment) {
  ITextAnalyticsAPI client = new TextAnalyticsAPI();
  client.AzureRegion = AzureRegions.Westcentralus;
  client.SubscriptionKey = Environment.GetEnvironmentVariable("TEXT_ANALYTICS_API_KEY", EnvironmentVariableTarget.Process);

  return (await client.SentimentAsync(
    new MultiLanguageBatchInput(
        new List<MultiLanguageInput>()
        {
          new MultiLanguageInput("en", "0", comment),
        })
  )).Documents.First().Score;
}
