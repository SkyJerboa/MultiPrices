using Microsoft.Extensions.Configuration;
using System;

namespace MP.Core.Common.Configuration
{
    public abstract class DefaultConfiguration
    {
        public string DefaultConnection { get; protected set; }

        public virtual void UpdateConfiguration(IConfiguration configuration)
        {
            DefaultConnection = configuration.GetConnectionString("DefaultConnection");
        }
    }
}
