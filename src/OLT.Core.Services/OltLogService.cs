////using System;
////using System.Diagnostics;
////using System.Globalization;
////using NLog;
////using NLog.Config;
////using OLT.Data.NlogRenderer;
////using OLT.Shared.Enums;
////using OLT.Shared.Interfaces;

////namespace OLT.Core
////{
////    public class OltLogService : Logger, IOltLogService
////    {
////        private const string LoggerName = "NLogLogger";
////        private const string WindowsLogName = "Application";

////        public static IOltLogService GetLoggingService()
////        {
////            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("utc_date", typeof(UtcDateRenderer));
////            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("web_variables", typeof(WebVariablesRenderer));
////            var logger = (IOltLogService)LogManager.GetLogger("NLogLogger", typeof(OltLogService));
////            return logger;
////        }

        
////        //public virtual bool IsSqlTraceEnabled => System.Configuration.ConfigurationManager.AppSettings["olt:sqlTrace"] == "true";

////        public virtual bool IsSqlTraceEnabled { get; set; } = false;

////        public void SqlTrace(string message)
////        {
////            if (IsSqlTraceEnabled)
////            {
////                Write(OltLogType.Trace, message);
////            }
////        }

////        public void Write(OltLogType logType, string message)
////        {
////            Write(logType, message, null);
////        }

////        public void Write(OltLogType logType, string message, string format, params object[] args)
////        {
////            var logger = LogManager.GetCurrentClassLogger();

////            var logEventInfo = GetLogEvent(LoggerName, LogLevel.FromOrdinal((int)logType), null, format, args);
////            logEventInfo.Message = message;
////            logger.Log(logEventInfo);

////            if (logType == OltLogType.Error || logType == OltLogType.Fatal)
////            {
////                ////if (System.Web.HttpContext.Current == null)
////                ////{
////                ////    ErrorLog.GetDefault(null).Log(new Error(new Exception(message)));
////                ////}
////                ////else
////                ////{
////                ////    ErrorSignal.FromCurrentContext().Raise(new Exception(message));
////                ////}
////            }

////        }

////        public void Write(Exception exception, string message, params object[] args)
////        {
////            var logger = LogManager.GetCurrentClassLogger();
////            logger.Log(LogLevel.Error, exception, CultureInfo.CurrentCulture, message, args);

////            var elmahError = $"{exception}{Environment.NewLine}{message}";
////            //if (System.Web.HttpContext.Current == null)
////            //{
////            //    ErrorLog.GetDefault(null).Log(new Error(new Exception(elmahError)));
////            //}
////            //else
////            //{
////            //    ErrorSignal.FromCurrentContext().Raise(new Exception(elmahError));
////            //}
////        }


////        public void Write(Exception exception)
////        {

////            try
////            {
////                var logger = LogManager.GetCurrentClassLogger();
////                logger.Log(LogLevel.Error, exception);
////            }
////            catch
////            {

////            }

////            ////if (System.Web.HttpContext.Current == null)
////            ////{
////            ////    ErrorLog.GetDefault(null).Log(new Error(exception));
////            ////}
////            ////else
////            ////{
////            ////    ErrorSignal.FromCurrentContext().Raise(exception);
////            ////}
////        }



////        public void Write(int eventId, string sourceName, string message, EventLogEntryType entryType)
////        {

////            if (!System.Diagnostics.EventLog.SourceExists(sourceName))
////            {
////                System.Diagnostics.EventLog.CreateEventSource(sourceName, WindowsLogName);
////            }

////            System.Diagnostics.EventLog.WriteEntry(sourceName, message, entryType, eventId <= 65535 ? eventId : 0);

////        }


////        private LogEventInfo GetLogEvent(string loggerName, LogLevel level, Exception exception, string format, params object[] args)
////        {
////            var assemblyProp = string.Empty;
////            var classProp = string.Empty;
////            var methodProp = string.Empty;
////            var messageProp = string.Empty;
////            var innerMessageProp = string.Empty;


////            var logEvent = !string.IsNullOrWhiteSpace(format) && args != null && args.Length > 0 ? new LogEventInfo(level, loggerName, string.Format(format, args)) :
////                new LogEventInfo(level, loggerName, null);

////            if (exception != null)
////            {
////                assemblyProp = exception.Source;
////                classProp = exception.TargetSite.DeclaringType.FullName;
////                methodProp = exception.TargetSite.Name;
////                messageProp = exception.Message;

////                if (exception.InnerException != null)
////                {
////                    innerMessageProp = exception.InnerException.Message;
////                }
////            }

////            logEvent.Properties["error-source"] = assemblyProp;
////            logEvent.Properties["error-class"] = classProp;
////            logEvent.Properties["error-method"] = methodProp;
////            logEvent.Properties["error-message"] = messageProp;
////            logEvent.Properties["inner-error-message"] = innerMessageProp;

////            return logEvent;
////        }

////        /// <summary>
////        /// The disposed
////        /// </summary>
////        protected bool Disposed { get; set; } = false;

////        /// <summary>
////        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
////        /// </summary>
////        public void Dispose()
////        {
////            Dispose(true);
////            GC.SuppressFinalize(this);
////        }


////        /// <summary>
////        /// Releases unmanaged and - optionally - managed resources.
////        /// </summary>
////        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
////        protected virtual void Dispose(bool disposing)
////        {
////            Disposed = true;
////        }
////    }
////}
