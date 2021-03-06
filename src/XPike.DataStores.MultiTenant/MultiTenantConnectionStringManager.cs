﻿using XPike.Configuration;
using XPike.Logging;
using XPike.MultiTenant;

namespace XPike.DataStores.MultiTenant
{
    public class MultiTenantConnectionStringManager
        : IMultiTenantConnectionStringManager
    {
        private readonly IConfig<ConnectionConfig> _config;
        private readonly ITenantContextAccessor _tenantAccessor;
        private readonly ILog<MultiTenantConnectionStringManager> _logger;

        public MultiTenantConnectionStringManager(ILog<MultiTenantConnectionStringManager> logger,
            IConfig<ConnectionConfig> config, ITenantContextAccessor tenantAccessor)
        {
            _config = config;
            _tenantAccessor = tenantAccessor;
            _logger = logger;
        }

        public string GetManagedConnectionString(string connectionString)
        {
            if (!_config.CurrentValue.Databases.TryGetValue(connectionString, out var db))
                throw new InvalidConfigurationException($"The multi-tenant connection string for the requested database '{connectionString}' was not defined.  Add the following configuration key: 'XPike:DataStores:MultiTenant:ConnectionConfig:{connectionString}:DEFAULT' and specify your connection string as the value.");

            var tenantId = _tenantAccessor.TenantContext?.Tenant?.UniqueId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.Trace($"No Tenant Context was found!  Attempting to use DEFAULT.");
                tenantId = DefaultTenantContextProvider.DEFAULT_CONTEXT_ID;
            }

            if (!db.TryGetValue(tenantId, out var connection))
            {
                if (tenantId == DefaultTenantContextProvider.DEFAULT_CONTEXT_ID)
                    throw new InvalidConfigurationException($"No connection string found for database '{connectionString}' and tenant '{tenantId}'.  Add the following key: 'XPike:DataStores:MultiTenant:ConnectionConfig:{connectionString}:{tenantId}' and specify your connection string as the value.");

                _logger.Trace($"No connection string found for database '{connectionString}' and tenant '{tenantId}' - attempting to use DEFAULT.  To specify a different connection string, add the following configuration key: 'XPike:DataStores:MultiTenant:ConnectionConfig:{connectionString}:{tenantId}' and specify your connection string as the value.");

                if (!db.TryGetValue(DefaultTenantContextProvider.DEFAULT_CONTEXT_ID, out connection))
                    throw new InvalidConfigurationException($"No connection string found for database '{connectionString}'.  Add the following key: 'XPike:DataStores:MultiTenant:ConnectionConfig:{connectionString}:DEFAULT' and specify your connection string as the value.");
            }

            return connection;
        }
    }
}