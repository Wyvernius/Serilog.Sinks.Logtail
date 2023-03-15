using System;
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Net.Http;
using System.Net.Mime;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Logtail
{
    public class LogtailSink : ILogEventSink, IDisposable
    {
        readonly IFormatProvider? _formatProvider;
        readonly HttpClient _client;

        public LogtailSink(IFormatProvider? formatProvider, string sourceToken)
        {
            _formatProvider = formatProvider;
            SourceToken = sourceToken;
            _client= new HttpClient();
            //_client.BaseAddress= new Uri("https://in.logtail.com/");
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sourceToken}");
        }

        public string Endpoint { get; set; } = "https://in.logtail.com";

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
				{ "message", logEvent.RenderMessage() },
			};
            foreach (var key in logEvent.Properties)
            {
                contextDictionary.Add(key.Key, key.Value.ToString());
            }
			var json = JsonConvert.SerializeObject(contextDictionary);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
            _client.PostAsync("https://in.logtail.com/", content);
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
