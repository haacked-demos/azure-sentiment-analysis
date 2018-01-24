#r "Newtonsoft.Json"

using System.Net;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Newtonsoft.Json;
using Octokit;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    string comment = data.comment.body;
    int repositoryId = data.repository.id;
    int issueNumber = data.issue.number;

    var sentimentScore = await AnalyzeSentiment(comment);

    string sentiment = "neutral";
    if (sentimentScore <= 0.2)
    {
       sentiment = "negative";
       await UpdateMessage(repositoryId, issueNumber, comment, "Hey now, let's keep it positive.");
    }
    if (sentimentScore >= 0.8) {
      sentiment = "positive";
      await UpdateMessage(repositoryId, data.issueNumber, comment, "Thanks for keeping it positive!");
    }

    log.Info($"Sentiment was '{sentiment}'. Comment: '{data.comment.body}' on '{data.comment.title}'");

    return req.CreateResponse(HttpStatusCode.OK, new {
        body = $"Sentiment: '{sentiment}'. Comment: '{data.comment.body}' on '{data.comment.title}'"
    });
}

static async Task<double?> AnalyzeSentiment(string comment)
{
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

static async Task UpdateMessage(int repositoryId, int issueNumber, string existingMessage, string sentimentMessage)
{
    var issueUpdate = new IssueUpdate {
        Body = $"{existingMessage}\n\n_Sentiment Bot Says: {sentimentMessage}_"
    };

    var client = new GitHubClient(new ProductHeaderValue("Haack-Sentiment-Bot", "0.1.0"));
    var personalAccessToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", EnvironmentVariableTarget.Process);
    client.Credentials = new Credentials(personalAccessToken);

    await client.Issue.Update(repositoryId, issueNumber, issueUpdate);
}
