using Microsoft.Extensions.Configuration;
using MP.Core.Common.Heplers;
using System;
using System.Linq;

namespace MP.Core.Common.Configuration
{
    public static class ConfigurationLoader
    {
        public static T LoadConfiguration<T>(IConfiguration configuration, out byte[] fileHash)
            where T : IConfigurationManager
        {
            fileHash = FileHelper.ComputeFileHash("appsettings.json");

            T configurationManager = (T)Activator.CreateInstance(typeof(T), new object[] { configuration });
            configurationManager.RefreshConfiguration();

            return configurationManager;
        }

        public static void ReloadConfiguration(IConfigurationManager configurationManager,  ref byte[] fileHash)
        {
            byte[] newHash = FileHelper.ComputeFileHash("appsettings.json");
            if (fileHash.SequenceEqual(newHash))
                return;

            fileHash = newHash;
            configurationManager.RefreshConfiguration();
        }
    }
}
