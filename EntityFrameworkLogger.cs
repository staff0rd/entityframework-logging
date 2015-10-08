using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Http;
using System.Data.Entity;

namespace Logging.EntityFramework
{
    public class EntityFrameworkLogger<TDbContext, TLog> : ILogger 
        where TLog : EntityFrameworkLog, new()
        where TDbContext : DbContext
    {
        const int _indentation = 2;
        readonly string _name;
        readonly Func<string, LogLevel, bool> _filter;
        readonly DbContext _db;
        readonly HttpContext _httpContext;

        public EntityFrameworkLogger(string name, Func<string, LogLevel, bool> filter, IServiceProvider serviceProvider)
        {
            _name = name;
            _filter = filter ?? GetFilter(serviceProvider.GetService<IOptions<EntityFrameworkLoggerOptions>>());
            _db = serviceProvider.GetRequiredService<TDbContext>();
            var accessor = serviceProvider.GetService<IHttpContextAccessor>();
            if (accessor != null)
                _httpContext = accessor.HttpContext;
        }

        private Func<string, LogLevel, bool> GetFilter(IOptions<EntityFrameworkLoggerOptions> options)
        {
            if (options != null)
            {
                return ((category, level) => GetFilter(options.Options, category, level));
            }
            else
                return ((category, level) => true);
        }

        private bool GetFilter(EntityFrameworkLoggerOptions options, string category, LogLevel level)
        {
            if (options.Filters != null)
            {
                var filter = options.Filters.Keys.FirstOrDefault(p => category.StartsWith(p));
                if (filter != null)
                    return (int)options.Filters[filter] <= (int)level;
                else return true;
            }
            return true;
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;
            var values = state as ILogValues;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else if (values != null)
            {
                var builder = new StringBuilder();
                FormatLogValues(
                    builder,
                    values,
                    level: 1,
                    bullet: false);
                message = builder.ToString();
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            else
            {
                message = LogFormatter.Formatter(state, exception);
            }
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var log = new TLog
            {
                Message = Trim(message, EntityFrameworkLog.MaximumMessageLength),
                Date = DateTime.UtcNow,
                Level = logLevel.ToString(),
                Logger = _name,
                Thread = eventId.ToString()
            };

            if (exception != null)
                log.Exception = Trim(exception.ToString(), EntityFrameworkLog.MaximumExceptionLength);

            if (_httpContext != null)
            {
                log.Browser = _httpContext.Request.Headers["User-Agent"];
                log.Username = _httpContext.User.Identity.Name;
                log.HostAddress = _httpContext.Connection.LocalIpAddress.ToString();
                log.Url = _httpContext.Request.Path;
                //LogProperty("Browser", request.UserAgent);
                //LogProperty("Username", user);
                //LogProperty("HostAddress", request.UserHostAddress);
                //LogProperty("Url", HttpContext.Current.Request.Url.AbsoluteUri);
            }

            _db.Set<TLog>().Add(log);

            try
            {
                _db.SaveChanges();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }

        }

        private static string Trim(string value, int maximumLength)
        {
            return value.Length > maximumLength ? value.Substring(0, maximumLength) : value;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_name, logLevel);
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
        }

        private void FormatLogValues(StringBuilder builder, ILogValues logValues, int level, bool bullet)
        {
            var values = logValues.GetValues();
            if (values == null)
            {
                return;
            }
            var isFirst = true;
            foreach (var kvp in values)
            {
                builder.AppendLine();
                if (bullet && isFirst)
                {
                    builder.Append(' ', level * _indentation - 1)
                           .Append('-');
                }
                else
                {
                    builder.Append(' ', level * _indentation);
                }
                builder.Append(kvp.Key)
                       .Append(": ");
                if (kvp.Value is IEnumerable && !(kvp.Value is string))
                {
                    foreach (var value in (IEnumerable)kvp.Value)
                    {
                        if (value is ILogValues)
                        {
                            FormatLogValues(
                                builder,
                                (ILogValues)value,
                                level + 1,
                                bullet: true);
                        }
                        else
                        {
                            builder.AppendLine()
                                   .Append(' ', (level + 1) * _indentation)
                                   .Append(value);
                        }
                    }
                }
                else if (kvp.Value is ILogValues)
                {
                    FormatLogValues(
                        builder,
                        (ILogValues)kvp.Value,
                        level + 1,
                        bullet: false);
                }
                else
                {
                    builder.Append(kvp.Value);
                }
                isFirst = false;
            }
        }

        //public IDisposable BeginScopeImpl(object state)
        //{
        //    return _provider.BeginScopeImpl(_name, state);
        //}

        //public bool IsEnabled(LogLevel logLevel)
        //{
        //    return _logger.IsEnabled(ConvertLevel(logLevel));
        //}

        //public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        //{
        //    var level = ConvertLevel(logLevel);
        //    if (!_logger.IsEnabled(level))
        //    {
        //        return;
        //    }

        //    var logger = _logger;
        //    string messageTemplate = null;

        //    var structure = state as ILogValues;
        //    if (structure != null)
        //    {
        //        foreach (var property in structure.GetValues())
        //        {
        //            if (property.Key == "{OriginalFormat}" && property.Value is string)
        //            {
        //                messageTemplate = (string)property.Value;
        //            }
        //            else if (property.Key.StartsWith("@"))
        //            {
        //                logger = logger.ForContext(property.Key.Substring(1), property.Value, destructureObjects: true);
        //            }
        //            else
        //            {
        //                logger = logger.ForContext(property.Key, property.Value);
        //            }
        //        }

        //        var stateType = state.GetType();
        //        var stateTypeInfo = stateType.GetTypeInfo();
        //        // Imperfect, but at least eliminates `1 and + names
        //        if (messageTemplate == null && !stateTypeInfo.IsGenericType && !stateTypeInfo.IsNested)
        //        {
        //            messageTemplate = "{" + stateType.Name + ":l}";
        //            logger = logger.ForContext(stateType.Name, LogFormatter.Formatter(state, null));
        //        }
        //    }

        //    if (messageTemplate == null && state != null)
        //    {
        //        messageTemplate = LogFormatter.Formatter(state, null);
        //    }

        //    if (string.IsNullOrEmpty(messageTemplate))
        //    {
        //        return;
        //    }

        //    if (eventId != 0)
        //    {
        //        logger = logger.ForContext("EventId", eventId, false);
        //    }

        //    logger.Write(level, exception, messageTemplate);
        //}

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

}

