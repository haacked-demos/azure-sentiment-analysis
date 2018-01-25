#r "Newtonsoft.Json"

using System.Net;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Newtonsoft.Json;
using Octokit;

/*
This method responds to the GitHub Web Hook POST request. It's expecting an
IssueComment event payload. https://developer.github.com/v3/activity/events/types/#issuecommentevent
*/
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    string action = data.action;

    // IMPORTANT!!! Since this webhook edits an issue comment, it needs to check
    // that the comment we're checking is a newly created comment and not an
    // edited comment or we could get into an infinite webhook loop.
    // Yes, this leaves a gap where a commenter can edit an innocuous comment
    // into an awful one and bypass this bot. Did I mention this is a proof of
    // concept?
    if (!action.Equals("created"))
    {
      return req.CreateResponse(HttpStatusCode.OK, new {
          body = $"Ignored comment that was '{action}'"
      });
    }

    string comment = data.comment.body;
    string issueTitle = data.issue.title;
    int repositoryId = data.repository.id;
    int issueNumber = data.issue.number;
    int commentId = data.comment.id;

    var sentimentScore = await AnalyzeSentiment(comment);

    log.Info($"Sentiment Score was '{sentimentScore}'. commentId: {commentId} repositoryId: '{repositoryId}'. issueNumber: '{issueNumber}'. Comment: '{comment}'. Title: '{issueTitle}'");

    string sentiment = "neutral";
    if (sentimentScore <= 0.2)
    {
       sentiment = "negative";
       await UpdateComment(repositoryId, commentId, comment, $"Hey now, let's keep it positive. (Score: {sentimentScore})");
    }
    if (sentimentScore >= 0.8) {
      sentiment = "positive";
      await UpdateComment(repositoryId, commentId, comment, $"Thanks for keeping it so positive! (Score: {sentimentScore})");
    }

    return req.CreateResponse(HttpStatusCode.OK, new {
        body = $"Sentiment: '{sentiment}'. Comment: '{comment}' on '{issueTitle}'"
    });
}

/*
Uses the client library in the Microsoft.Azure.CognitiveServices.Language NuGet
package to call the Text Analytics API https://azure.microsoft.com/en-us/services/cognitive-services/text-analytics/
We only care about the sentiment analysis. We're also assuming English for now.
*/
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

/*
Uses Octokit to update an existing issue comment.
*/
static async Task UpdateComment(long repositoryId, int commentId, string existingCommentBody, string sentimentMessage)
{
    var client = new GitHubClient(new ProductHeaderValue("haack-test-bot", "0.1.0"));
    var personalAccessToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", EnvironmentVariableTarget.Process);
    client.Credentials = new Credentials(personalAccessToken);

    await client.Issue.Comment.Update(repositoryId, commentId, $"{existingCommentBody}\n\n_Sentiment Bot Says: {sentimentMessage}_");
}
