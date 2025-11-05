using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace YandexAdsEditor
{
    public static class AdapterDataLoader
    {
        public static void LoadAdapterData(List<AdapterInfo> adapters, List<string> baseAdapterNames)
        {
            adapters.Clear();

            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/YandexMobileAds/Editor");

            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning("Required directory for adapter data not found. Please ensure the plugin is correctly installed.");
                return;
            }

            string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml");

            Dictionary<string, (string AndroidVersion, string IOSVersion)> connectedAdapters = new Dictionary<string, (string, string)>();

            foreach (string xmlFile in xmlFiles)
            {
                if (Path.GetFileName(xmlFile).Equals("YandexMobileadsDependencies.xml") || (Path.GetFileName(xmlFile).Equals("YandexMobileadsMediationDependencies.xml")))
                {
                    continue;
                }
                try
                {
                 
                    var doc = XDocument.Load(xmlFile);

                    var androidPackage = doc.Descendants("androidPackage").FirstOrDefault();
                    string androidVersion = androidPackage?.Attribute("spec")?.Value.Split(':').LastOrDefault() ?? "—";

                    var iosPod = doc.Descendants("iosPod").FirstOrDefault();
                    string iosVersion = iosPod?.Attribute("version")?.Value ?? "—";

                    string adapterName = Path.GetFileNameWithoutExtension(xmlFile)
                        .Replace("YandexMobileadsDependencies", "")
                        .Replace("Mobileads", "")
                        .Replace("MediationDependencies", "")
                        .ToLower();

                    connectedAdapters[adapterName] = (androidVersion, iosVersion);
                }
                catch
                {
                    Debug.LogWarning($"An error occurred while processing adapter data. Please check the XML file: {Path.GetFileName(xmlFile)}.");
                }
            }

            if (baseAdapterNames.Count == 0)
            {
                foreach (var kvp in connectedAdapters)
                {
                    string displayName = kvp.Key;
                    adapters.Add(new AdapterInfo(displayName, kvp.Value.AndroidVersion, kvp.Value.IOSVersion, true));
                }
                return;
            }

            foreach (string adapterName in baseAdapterNames)
            {
                string lowerCaseName = adapterName.ToLower();
                if (connectedAdapters.TryGetValue(lowerCaseName, out var versions))
                {
                    adapters.Add(new AdapterInfo(adapterName, versions.AndroidVersion, versions.IOSVersion, true));
                }
                else
                {
                    adapters.Add(new AdapterInfo(adapterName, "—", "—", false));
                }
            }
        }
    }
}
