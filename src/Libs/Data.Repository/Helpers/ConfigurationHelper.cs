using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Helpers
{
    public static class ConfigurationHelper
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            return Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
                configuration.GetConnectionString("PostgresConnection");
        }
    }
}
