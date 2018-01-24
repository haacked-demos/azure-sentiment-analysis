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
    string issueTitle = data.issue.title;
    int repositoryId = data.repository.id;
    int issueNumber = data.issue.number;
    int commentId = data.comment.id;

    var sentimentScore = await AnalyzeSentiment(comment);

    log.Info($"Sentiment Score was '{sentimentScore}'. commentId: {commentId} repositoryId: '{repositoryId}'. issueNumber: '{issueNumber}'. Comment: '{comment}'. Title: '{issueTitle}'");

    string sentiment = "neutral";
    if (sentimentScore <= 0.1)
    {
       sentiment = "negative";
       await UpdateComment(repositoryId, commentId, comment, "Hey now, let's keep it positive.");
    }
    if (sentimentScore >= 0.9) {
      sentiment = "positive";
      await UpdateComment(repositoryId, commentId, comment, "Thanks for keeping it positive!");
    }

    return req.CreateResponse(HttpStatusCode.OK, new {
        body = $"Sentiment: '{sentiment}'. Comment: '{comment}' on '{issueTitle}'"
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

static async Task UpdateComment(long repositoryId, int commentId, string existingCommentBody, string sentimentMessage)
{
    var client = new GitHubClient(new ProductHeaderValue("haack-test-bot", "0.1.0"));
    var personalAccessToken = "53575a53426c714c2480d7e459b00dde4b1cf897";
    client.Credentials = new Credentials(personalAccessToken);

    await client.Issue.Comment.Update(repositoryId, commentId, $"{existingCommentBody}\n\n_Sentiment Bot Says: {sentimentMessage}_");
}
