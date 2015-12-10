using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity;

namespace Atquin.EntityFramework.Logging
{
    public static class EntityFrameworkLoggerFactoryExtensions
    {
        public static ILoggerFactory AddEntityFramework<TDbContext, TLog>(this ILoggerFactory factory, IServiceProvider serviceProvider, Func<string, LogLevel, bool> filter = null)
            where TDbContext : DbContext
            where TLog : EntityFrameworkLog, new()
        {
            if (factory == null) throw new ArgumentNullException("factory");

            factory.AddProvider(new EntityFrameworkLoggerProvider<TDbContext, TLog>(serviceProvider, filter));

            return factory;
        }
    }
}
