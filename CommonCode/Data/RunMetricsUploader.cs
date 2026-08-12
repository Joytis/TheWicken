using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;

namespace TheWitch.CommonCode.Data;

/// <summary>
/// Fire-and-forget JSON POST uploader for a Supabase REST endpoint (PostgREST insert).
/// Failures are logged and dropped — analytics must never affect gameplay.
/// </summary>
internal sealed class RunMetricsUploader
{
    private static readonly HttpClient s_client = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly string _endpoint;
    private readonly string _apiKey;

    public RunMetricsUploader(string endpoint, string apiKey)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_apiKey);

    public void Upload(string json, string context)
    {
        if (!IsConfigured)
        {
            TheWitchCode.MainFile.Logger.Info($"Analytics upload for '{context}' skipped: no endpoint configured.");
            return;
        }
        TaskHelper.RunSafely(PostRequest(json, context));
    }

    private async Task PostRequest(string json, string context)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, _endpoint);
        request.Headers.Add("apikey", _apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await s_client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TheWitchCode.MainFile.Logger.Warn($"Analytics upload for '{context}' failed due to network error: {ex.Message}");
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            TheWitchCode.MainFile.Logger.Info($"Analytics for '{context}' successfully uploaded.");
        }
        else
        {
            string body = await response.Content.ReadAsStringAsync();
            TheWitchCode.MainFile.Logger.Warn($"Analytics upload for '{context}' failed with status {response.StatusCode}: {body}");
        }
    }
}
