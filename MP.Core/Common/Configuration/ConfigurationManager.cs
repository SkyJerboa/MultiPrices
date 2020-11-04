using Microsoft.Extensions.Configuration;

namespace MP.Core.Common.Configuration
{
    public abstract class ConfigurationManager<T> : IConfigurationManager 
        where T : DefaultConfiguration, new()
    {
        public static T Config { get; protected set; }
        public static string DefaultConnection { get => Config.DefaultConnection; }

        public delegate void ConfigHandler();
        public static event ConfigHandler OnReloadConfig;

        protected static ConfigurationManager<T> _thisManager;

        protected IConfiguration _netConfiguration;

        public ConfigurationManager(IConfiguration configuration)
        {
            if (_thisManager != null || configuration == null)
                return;

            Config = new T();
            _thisManager = this;
            _netConfiguration = configuration;
        }

        public virtual void RefreshConfiguration()
        {
            Config.UpdateConfiguration(_thisManager._netConfiguration);
            OnReloadConfig?.Invoke();
        }
    }
}
