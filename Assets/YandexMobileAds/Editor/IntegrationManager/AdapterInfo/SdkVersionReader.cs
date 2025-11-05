using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace YandexAdsEditor
{
    public static class SdkVersionReader
    {
        private const string DependenciesFilePath = "Assets/YandexMobileAds/Editor/YandexMobileadsDependencies.xml";
        private const string MediationDependenciesFilePath = "Assets/YandexMobileAds/Editor/YandexMobileadsMediationDependencies.xml";

        public static string GetSdkVersion()
        {
            string version = GetVersionFromFile(DependenciesFilePath);
            if (version == "Unknown")
            {
                version = GetVersionFromFile(MediationDependenciesFilePath);
            }

            return version;
        }

        private static string GetVersionFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "Unknown";
            }

            try
            {
                var doc = XDocument.Load(filePath);

                var iosPod = doc.Descendants("iosPod").FirstOrDefault();

                if (iosPod == null || iosPod.Attribute("version") == null)
                {
                    return "Unknown";
                }

                return iosPod.Attribute("version")?.Value ?? "Unknown";
            }
            catch (System.Exception)
            {
                return "Error";
            }
        }
    }
}