using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace YandexAdsEditor
{
    public static class GitHubAdapterFetcher
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/yandexmobile/yandex-ads-unity-plugin/contents/mobileads-mediation";

        public static async Task FetchAdaptersAsync(List<string> baseAdapterNames)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Unity Editor");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(GitHubApiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"Error querying GitHub API: {response.StatusCode} - {response.ReasonPhrase}");
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    ParseGitHubResponse(jsonResponse, baseAdapterNames);

                    Debug.Log("Adapter list updated from GitHub.");
                }
                catch (HttpRequestException httpEx)
                {
                    Debug.LogError($"HTTP request error: {httpEx.Message}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error processing GitHub API response: {ex.Message}");
                }
            }
        }

        private static void ParseGitHubResponse(string jsonResponse, List<string> baseAdapterNames)
        {
            jsonResponse = "{ \"responses\": " + jsonResponse + " }";

            var container = JsonUtility.FromJson<GitHubResponseContainer>(jsonResponse);
            baseAdapterNames.Clear();

            foreach (var item in container.responses)
            {
                if (item.type == "dir")
                {
                    baseAdapterNames.Add(item.name);
                }
            }
        }

        [System.Serializable]
        private class GitHubResponse
        {
            public string name;
            public string type;
        }

        [System.Serializable]
        private class GitHubResponseContainer
        {
            public GitHubResponse[] responses;
        }
    }
}
