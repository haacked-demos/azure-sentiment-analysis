# Haacked Azure Functions

This repository hosts the configuration and code for my investigations into "Serverless" programming using Azure Functions.

One nice feature of Azure Functions is it's easy to set up [continuous deployment from GitHub to Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-continuous-deployment). So that's what I did!

The `sentiment-analysis-webhook` function is a GitHub Webhook that calls into Microsoft's Text Analysis service to perform sentiment analysis on issue comments.

For more information, check out [my blog post about this](https://haacked.com/archive/2018/01/27/analyze-github-issue-comment-sentiment/).
