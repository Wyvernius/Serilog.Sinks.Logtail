using System;
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Serilog.Sinks.PeriodicBatching;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Serilog.Sinks.Logtail
{
    public class SlidecrewLogSink : IBatchedLogEventSink, ILogEventSink, IDisposable
    {
        readonly IFormatProvider? _formatProvider;
        readonly HttpClient _client;

        public SlidecrewLogSink(IFormatProvider? formatProvider, string sourceToken)
        {
            _formatProvider = formatProvider;
            SourceToken = sourceToken;
            _client= new HttpClient();
            //_client.BaseAddress= new Uri("https://in.logtail.com/");
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sourceToken}");
        }

        public string Endpoint { get; set; } = "https://logging.slidecrew.net/api/v1/ingest_logs";

        public int Retries { get; set; }

        public double FlushPeriodMilliseconds { get; set; }

        public int MaxBatchSize { get; set; }

        public string SourceToken { get; set; }

        public void Dispose()
        {
            
        }

        public void Emit(LogEvent logEvent)
        {
            Dictionary<string, string> contextDictionary = new Dictionary<string, string>
            {
                      { "dt", DateTime.UtcNow.ToString("o") },
                    { "message", logEvent.RenderMessage() },
                    { "Level", logEvent.Level.ToString() },
                    { "Exception", logEvent.Exception == null ? null : logEvent.Exception.StackTrace },
                    { "Platform", RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unknown" } 
            };

			foreach (var key in logEvent.Properties)
            {
                contextDictionary.Add(key.Key, key.Value.ToString());
            }
			var json = JsonConvert.SerializeObject(contextDictionary);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
            _client.PostAsync(Endpoint, content);
		}


        public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            List<Dictionary<string, string>> logs = new List<Dictionary<string, string>>();

            foreach (var logEvent in batch)
            {
                Dictionary<string, string> contextDictionary = new Dictionary<string, string>
                {
                    { "dt", DateTime.UtcNow.ToString("o") },
                    { "message", logEvent.RenderMessage() },
                    { "Level", logEvent.Level.ToString() },
                    { "Exception", logEvent.Exception == null ? null : logEvent.Exception.StackTrace },
                    { "Platform", RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unknown" }
                };
                foreach (var key in logEvent.Properties)
                {
                    contextDictionary.Add(key.Key, key.Value.ToString());
                }
                logs.Add(contextDictionary);
            }

			var json = JsonConvert.SerializeObject(logs);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			return _client.PostAsync("https://logging.slidecrew.net/api/v1/ingest_logs", content);
		}

		public Task OnEmptyBatchAsync()
		{
			return Task.CompletedTask;
		}
	}

    public static class SlidecrewSeqSinkExtensions
    {
        public static LoggerConfiguration SlidecrewLog(
            this LoggerSinkConfiguration loggerConfiguration,
            string sourceToken,
            IFormatProvider? formatProvider = null,
            int flushPeriodMilliseconds = 250,
            int maxBatchSize = 1000)
        {
            var sink = new SlidecrewLogSink(formatProvider, sourceToken);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = maxBatchSize,
                Period = TimeSpan.FromMilliseconds(flushPeriodMilliseconds)
            };

            var batchSink = new PeriodicBatchingSink(sink, batchingOptions);

            return loggerConfiguration.Sink(sink);
        }
    }
}
