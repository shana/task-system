using System;
using System.Linq;

namespace GitHub.Unity
{
    class NullLogAdapter : LogAdapterBase
    {
        public override void Info(string context, string message)
        {
        }

        public override void Debug(string context, string message)
        {
        }

        public override void Trace(string context, string message)
        {
        }

        public override void Warning(string context, string message)
        {
        }

        public override void Error(string context, string message)
        {
        }
    }

    static class Logging
    {
        private static readonly LogAdapterBase nullLogAdapter = new NullLogAdapter();

        private static bool tracingEnabled;
        public static bool TracingEnabled
        {
            get
            {
                return tracingEnabled;
            }
            set
            {
                if (tracingEnabled != value)
                {
                    tracingEnabled = value;
                    Instance?.Info("Trace Logging " + (value ? "Enabled" : "Disabled"));
                }
            }
        }

        private static LogAdapterBase logAdapter = nullLogAdapter;

        public static LogAdapterBase LogAdapter
        {
            get { return logAdapter; }
            set { logAdapter = value ?? nullLogAdapter; }
        }

        private static ILogging instance;

        private static ILogging Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GetLogger();
                }
                return instance;
            }
            set { instance = value; }
        }

        public static ILogging GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ILogging GetLogger(Type type)
        {
            return GetLogger(type.Name);
        }

        public static ILogging GetLogger(string context = null)
        {
            return new LogFacade($"<{context ?? "Global"}>");
        }

        public static void Info(string s)
        {
            Instance.Info(s);
        }

        public static void Debug(string s)
        {
            Instance.Debug(s);
        }

        public static void Trace(string s)
        {
            Instance.Trace(s);
        }

        public static void Warning(string s)
        {
            Instance.Warning(s);
        }

        public static void Error(string s)
        {
            Instance.Error(s);
        }

        public static void Error(Exception exception)
        {
            Instance.Error(exception);
        }

        public static void Info(string format, params object[] objects)
        {
            Instance.Info(format, objects);
        }

        public static void Debug(string format, params object[] objects)
        {
            Instance.Debug(format, objects);
        }

        public static void Trace(string format, params object[] objects)
        {
            Instance.Trace(format, objects);
        }

        public static void Warning(string format, params object[] objects)
        {
            Instance.Warning(format, objects);
        }

        public static void Error(string format, params object[] objects)
        {
            Instance.Error(format, objects);
        }

        public static void Info(Exception ex, string s)
        {
            Instance.Info(ex, s);
        }

        public static void Debug(Exception ex, string s)
        {
            Instance.Debug(ex, s);
        }

        public static void Trace(Exception ex, string s)
        {
            Instance.Trace(ex, s);
        }

        public static void Warning(Exception ex, string s)
        {
            Instance.Warning(ex, s);
        }

        public static void Error(Exception ex, string s)
        {
            Instance.Error(ex, s);
        }
    }

    class LogFacade : ILogging
    {
        private readonly string context;

        public LogFacade(string context)
        {
            this.context = context;
        }

        public void Info(string message)
        {
            Logging.LogAdapter.Info(context, message);
        }

        public void Debug(string message)
        {
#if DEBUG
            Logging.LogAdapter.Debug(context, message);
#endif
        }

        public void Trace(string message)
        {
            if (!Logging.TracingEnabled) return;
            Logging.LogAdapter.Trace(context, message);
        }

        public void Info(string format, params object[] objects)
        {
            Info(String.Format(format, objects));
        }

        public void Info(Exception ex, string message)
        {
            Info(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Info(Exception ex)
        {
            Info(ex, string.Empty);
        }

        public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, String.Format(format, objects));
        }

        public void Debug(string format, params object[] objects)
        {
#if DEBUG
            Debug(String.Format(format, objects));
#endif
        }

        public void Debug(Exception ex, string message)
        {
#if DEBUG
            Debug(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
#endif
        }

        public void Debug(Exception ex)
        {
#if DEBUG
            Debug(ex, string.Empty);
#endif
        }

        public void Debug(Exception ex, string format, params object[] objects)
        {
#if DEBUG
            Debug(ex, String.Format(format, objects));
#endif
        }

        public void Trace(string format, params object[] objects)
        {
            if (!Logging.TracingEnabled) return;

            Trace(String.Format(format, objects));
        }

        public void Trace(Exception ex, string message)
        {
            if (!Logging.TracingEnabled) return;

            Trace(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Trace(Exception ex)
        {
            if (!Logging.TracingEnabled) return;

            Trace(ex, string.Empty);
        }

        public void Trace(Exception ex, string format, params object[] objects)
        {
            if (!Logging.TracingEnabled) return;

            Trace(ex, String.Format(format, objects));
        }

        public void Warning(string message)
        {
            Logging.LogAdapter.Warning(context, message);
        }

        public void Warning(string format, params object[] objects)
        {
            Warning(String.Format(format, objects));
        }

        public void Warning(Exception ex, string message)
        {
            Warning(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Warning(Exception ex)
        {
            Warning(ex, string.Empty);
        }

        public void Warning(Exception ex, string format, params object[] objects)
        {
            Warning(ex, String.Format(format, objects));
        }

        public void Error(string message)
        {
            Logging.LogAdapter.Error(context, message);
        }

        public void Error(string format, params object[] objects)
        {
            Error(String.Format(format, objects));
        }

        public void Error(Exception ex, string message)
        {
            Error(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Error(Exception ex)
        {
            Error(ex, string.Empty);
        }

        public void Error(Exception ex, string format, params object[] objects)
        {
            Error(ex, String.Format(format, objects));
        }
    }

    static class ExceptionExtensions
    {
        public static string GetExceptionMessage(this Exception ex)
        {
            var message = ex.Message + Environment.NewLine + ex.StackTrace;
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (stack.Length > 2)
            {
                message = message + Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(2).ToArray());
            }
            return message;
        }
    }
}