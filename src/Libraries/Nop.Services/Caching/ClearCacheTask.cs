﻿using Nop.Core.Caching;
using Nop.Core.Infrastructure;
using Nop.Services.Tasks;

namespace Nop.Services.Caching
{
    /// <summary>
    /// Clear cache schedueled task implementation
    /// </summary>
    public partial class ClearCacheTask : ITask
    {
        /// <summary>
        /// Executes a task
        /// </summary>
        public void Execute()
        {
            EngineContext.Current.Resolve<IStaticCacheManager>().Clear();
        }
    }
}
