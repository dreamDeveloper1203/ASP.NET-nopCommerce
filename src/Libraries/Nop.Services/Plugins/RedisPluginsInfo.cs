﻿using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Redis;
using StackExchange.Redis;

namespace Nop.Services.Plugins
{
    /// <summary>
    /// Represents an information about plugins
    /// </summary>
    public partial class RedisPluginsInfo : PluginsInfo
    {
        #region Fields

        private readonly IDatabase _db;

        #endregion

        #region Ctor

        public RedisPluginsInfo(INopFileProvider fileProvider, IRedisConnectionWrapper connectionWrapper, NopConfig config)
            : base(fileProvider)
        {
            _db = connectionWrapper.GetDatabase(config.RedisDatabaseId ?? (int)RedisDatabaseNumber.Plugin).Result;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Save plugins info to the redis
        /// </summary>
        public override async Task Save()
        {
            var text = JsonConvert.SerializeObject(this, Formatting.Indented);
            await _db.StringSetAsync(nameof(RedisPluginsInfo), text);
        }

        /// <summary>
        /// Get plugins info
        /// </summary>
        /// <returns>True if data are loaded, otherwise False</returns>
        public override async Task<bool> LoadPluginInfo()
        {
            //try to get plugin info from the JSON file
            var serializedItem = await _db.StringGetAsync(nameof(RedisPluginsInfo));

            var loaded = false;

            if (serializedItem.HasValue) 
                loaded = DeserializePluginInfo(serializedItem);

            if (loaded)
                return true;

            if (await base.LoadPluginInfo())
            {
                await Save();
                loaded = true;
            }

            //delete the plugins info file
            var filePath = _fileProvider.MapPath(NopPluginDefaults.PluginsInfoFilePath);
            _fileProvider.DeleteFile(filePath);
            
            return loaded;
        }

        #endregion
    }
}
