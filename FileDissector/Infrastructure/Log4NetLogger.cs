using System;
using FileDissector.Domain.Infrastructure;
using log4net;

namespace FileDissector.Infrastructure
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;
        public Log4NetLogger(Type type)
        {
            _log = LogManager.GetLogger(type);
        }

        public void Debug(string message, params object[] values)
        {
            if (!_log.IsDebugEnabled) return;
            _log.DebugFormat(message, values);
        }

        public void Info(string message, params object[] values)
        {
            if (!_log.IsInfoEnabled) return;
            _log.InfoFormat(message, values);
        }

        public void Warn(string message, params object[] values)
        {
            if (!_log.IsWarnEnabled) return;
            _log.WarnFormat(message, values);
        }

        public void Fatal(string message, params object[] values)
        {
            if (!_log.IsFatalEnabled) return;
            _log.FatalFormat(message, values);
        }

        public void Error(Exception ex, string message, params object[] values)
        {
            if (!_log.IsErrorEnabled) return;
            _log.Error(string.Format(message, values), ex);
        }
    }
}
