using System;
using System.Linq;
using Logtail;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Logtail
{
    public class LogtailSink : ILogEventSink, IDisposable
    {
        readonly IFormatProvider? _formatProvider;
        readonly Drain _logtail;

        public LogtailSink(IFormatProvider? formatProvider, string sourceToken)
        {
            _formatProvider = formatProvider;
            SourceToken = sourceToken;

            var client = new Client(
                SourceToken,
                endpoint: Endpoint,
                retries: Retries
            );

            _logtail = new Drain(
                client,
                period: TimeSpan.FromMilliseconds(FlushPeriodMilliseconds),
                maxBatchSize: MaxBatchSize
            );
        }

        public string Endpoint { get; set; } = "https://in.logtail.com";

        public int Retries { get; set; }

        public double FlushPeriodMilliseconds { get; set; }

        public int MaxBatchSize { get; set; }

        public string SourceToken { get; set; }

        public void Dispose()
        {
            _logtail?.Stop().Wait();
        }

        public void Emit(LogEvent logEvent)
        {
            var contextDictionary = logEvent.Properties.ToDictionary(x => x.Key, x => (object)x);

            var log = new global::Logtail.Log()
            {
                Timestamp = logEvent.Timestamp,
                Message = logEvent.RenderMessage(_formatProvider),
                Level = logEvent.Level.ToString(),
                Context = contextDictionary,
            };

            _logtail.Enqueue(log);
        }
    }

    public static class LogtailSeqSinkExtensions
    {
        public static LoggerConfiguration LogtailSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string sourceToken,
            IFormatProvider? formatProvider = null,
            string endpoint = "https://in.logtail.com",
            int retries = 10,
            int flushPeriodMilliseconds = 250,
            int maxBatchSize = 1000)
        {
            return loggerConfiguration.Sink(new LogtailSink(formatProvider, sourceToken)
                { Endpoint = endpoint, Retries = retries, FlushPeriodMilliseconds = flushPeriodMilliseconds, MaxBatchSize = maxBatchSize });
        }
    }
}
