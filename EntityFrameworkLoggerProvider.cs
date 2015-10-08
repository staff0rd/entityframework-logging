using Microsoft.Framework.Logging;
using System;
using System.Data.Entity;

namespace Logging.EntityFramework
{
    public class EntityFrameworkLoggerProvider<TDbContext, TLog> : ILoggerProvider 
        where TLog : EntityFrameworkLog, new()
        where TDbContext : DbContext
    {
        readonly Func<string, LogLevel, bool> _filter;
        readonly IServiceProvider _serviceProvider;

        public EntityFrameworkLoggerProvider(IServiceProvider serviceProvider, Func<string, LogLevel, bool> filter)
        {
            _filter = filter;
            _serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string name)
        {
            return new EntityFrameworkLogger<TDbContext, TLog>(name, _filter, _serviceProvider);
        }

        public void Dispose() { }
    }
}
