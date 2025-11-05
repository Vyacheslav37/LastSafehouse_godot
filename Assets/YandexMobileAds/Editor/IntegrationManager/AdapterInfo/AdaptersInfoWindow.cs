using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;

namespace YandexAdsEditor
{
    public class AdaptersInfoWindow : EditorWindow
    {
        private List<AdapterInfo> adapters = new List<AdapterInfo>();
        private static List<string> BaseAdapterNames = new List<string>();
        private const string GitHubApiUrl = "https://api.github.com/repos/yandexmobile/yandex-ads-unity-plugin/contents/mobileads-mediation";
        private const string BaseRawUrl = "https://raw.githubusercontent.com/yandexmobile/yandex-ads-unity-plugin/master/mobileads-mediation/";
        private string latestSdkVersion = "Unknown";
        private bool isLoading = true;
        private string adapterConnecting = null;

        [MenuItem("YandexAds/Adapters Info")]
        public static void ShowWindow()
        {
            GetWindow<AdaptersInfoWindow>("Adapters Info");
        }

        private void OnEnable()
        {
            AssetDatabase.importPackageCompleted += OnPackageImported;
            _ = FetchAndLoadAdaptersAsync();
        }

        private void OnDisable()
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
        }

        private async Task FetchAndLoadAdaptersAsync()
        {
            isLoading = true;
            adapterConnecting = null;
            Repaint();

            latestSdkVersion = await SdkUpdater.GetLatestSdkVersionFromChangelogAsync();
            await GitHubAdapterFetcher.FetchAdaptersAsync(BaseAdapterNames);

            if (latestSdkVersion != "Unknown" && BaseAdapterNames.Count > 0)
            {
                BaseAdapterNames = await FilterAdaptersByLatestVersion(latestSdkVersion, BaseAdapterNames, 5);
            }

            LoadLocalAdapterData();
            isLoading = false;
            Repaint();
        }

        private void LoadLocalAdapterData()
        {
            AdapterDataLoader.LoadAdapterData(adapters, BaseAdapterNames);
        }

        private void OnGUI()
        {
            string currentVersion = SdkVersionReader.GetSdkVersion();

            if (isLoading)
            {
                GUILayout.Label("Loading...");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label("SDK Version: " + currentVersion);
            if (latestSdkVersion == currentVersion && latestSdkVersion != "Unknown")
            {
                GUILayout.Label("You have the latest version!", EditorStyles.label);
            }
            else
            {
                GUILayout.Label(latestSdkVersion == "Unknown" ? "(Please check your internet connection!)" : "(Latest version: " + latestSdkVersion + ")", EditorStyles.label);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Space(10);
            bool isLatestKnownAndSame = (latestSdkVersion != "Unknown" && latestSdkVersion == currentVersion);
            GUI.enabled = !isLatestKnownAndSame;
            if (GUILayout.Button("Update SDK", GUILayout.Width(120)))
            {
                UpdateSdk();
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        

            string adapterToConnect = null;
            if (latestSdkVersion == currentVersion) {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Adapter Name", GUILayout.Width(300));
                GUILayout.Label("Android Version", GUILayout.Width(150));
                GUILayout.Label("iOS Version", GUILayout.Width(150));
                GUILayout.Label("Status", GUILayout.Width(100));
                GUILayout.Label("", GUILayout.Width(100));
                GUILayout.EndHorizontal();

                if (adapters.Count == 0)
                {
                    GUILayout.Label("No adapters available for this SDK version.");
                    return;
                }

                for (int i = 0; i < adapters.Count; i++)
                {
                    var adapter = adapters[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(adapter.Name, GUILayout.Width(300));
                    GUILayout.Label(adapter.AndroidVersion, GUILayout.Width(150));
                    GUILayout.Label(adapter.IOSVersion, GUILayout.Width(150));
                    GUILayout.Label(adapter.IsConnected ? "Connected" : "Not Connected", GUILayout.Width(100));

                    if (adapter.Name == "google")
                    {
                        if (adapter.IsConnected)
                        {
                            if (GUILayout.Button("Setting", GUILayout.Width(100)))
                            {
                                OpenAdapterSettings(adapter);
                            }
                        }
                        else if (latestSdkVersion != "Unknown")
                        {
                            GUI.enabled = adapterConnecting != adapter.Name;
                            if (GUILayout.Button("Connect", GUILayout.Width(100)))
                            {
                                adapterToConnect = adapter.Name;
                            }
                            GUI.enabled = true;
                        }
                    }
                    else
                    {
                        if (!adapter.IsConnected && latestSdkVersion != "Unknown")
                        {
                            GUI.enabled = adapterConnecting != adapter.Name;
                            if (GUILayout.Button("Connect", GUILayout.Width(100)))
                            {
                                adapterToConnect = adapter.Name;
                            }
                            GUI.enabled = true;
                        }
                        else
                        {
                            GUILayout.Space(100);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            } else {
                GUILayout.Label("Please update SDK to the latest version for managing adapters.");
                return;
            }

            if (!string.IsNullOrEmpty(adapterToConnect))
            {
                AdapterDownloader.DownloadAndImport(adapterToConnect, currentVersion);
                AssetDatabase.Refresh();
            }
        }

        private async void UpdateSdk()
        {
            await SdkUpdater.UpdateSdkAsync();
            Close();
            string newVersion = SdkVersionReader.GetSdkVersion();
            UpdateConnectedAdapters(newVersion);
            _ = FetchAndLoadAdaptersAsync();
        }

        private void UpdateConnectedAdapters(string newVersion)
        {
            foreach (var adapter in adapters)
            {
                if (adapter.IsConnected)
                {
                    AdapterDownloader.DownloadAndImport(adapter.Name, newVersion);
                }
            }
        }

        private async Task<List<string>> FilterAdaptersByLatestVersion(string latestSdkVersion, List<string> baseAdapterNames, int maxConcurrentRequests)
        {
            var filtered = new List<string>();
            var semaphore = new SemaphoreSlim(maxConcurrentRequests);
            var tasks = new List<Task>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Unity Editor");
                var lockObj = new object();

                foreach (string adapterName in baseAdapterNames)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            bool supported = await IsAdapterVersionSupportedAsync(client, adapterName, latestSdkVersion);
                            if (supported)
                            {
                                lock (lockObj)
                                {
                                    filtered.Add(adapterName);
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }

            return filtered;
        }

        private async Task<bool> IsAdapterVersionSupportedAsync(HttpClient client, string adapterName, string version)
        {
            string lowerCaseName = adapterName.ToLower();
            string url = $"https://raw.githubusercontent.com/yandexmobile/yandex-ads-unity-plugin/master/mobileads-mediation/{lowerCaseName}/mobileads-{lowerCaseName}-mediation-{version}.unitypackage";

            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void OnPackageImported(string packageName)
        {   
            LoadLocalAdapterData();
            Repaint();
        }

        private void OpenAdapterSettings(AdapterInfo adapter)
        {
            GoogleSettingsWindow.ShowWindow(adapter);
        }
    }
}
