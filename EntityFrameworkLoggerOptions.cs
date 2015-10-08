using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logging.EntityFramework
{
    public class EntityFrameworkLoggerOptions
    {
        public Dictionary<string, LogLevel> Filters { get; set; }
    }
}
