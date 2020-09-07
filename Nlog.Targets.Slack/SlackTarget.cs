﻿using Nlog.Targets.Slack.Models;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nlog.Targets.Slack
{
    [Target("Slack")]
    public class SlackTarget : TargetWithContext
    {
        private Layout _uri = "http://slack.is.awesome.com";

        [RequiredParameter]
        public string WebHookUrl
        {
            get => (_uri as SimpleLayout)?.Text;
            set
            {
                _uri = value ?? string.Empty;
                if (IsInitialized)
                {
                    InitializeTarget();
                }
            }
        }

        private string _WebHookUrl;


        public bool Compact { get; set; }

        public override IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

        [ArrayParameter(typeof(TargetPropertyWithContext), "field")]
        public IList<TargetPropertyWithContext> Fields => ContextProperties;

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            var eventInfo = LogEventInfo.CreateNullEvent();
            _WebHookUrl = _uri?.Render(eventInfo) ?? string.Empty;
            if (String.IsNullOrWhiteSpace(_WebHookUrl))
                throw new ArgumentOutOfRangeException("WebHookUrl", "Webhook URL cannot be empty.");

            if (!this.Compact && this.ContextProperties.Count == 0)
            {
                this.ContextProperties.Add(new TargetPropertyWithContext("Process Name", Layout = "${machinename}\\${processname}"));
                this.ContextProperties.Add(new TargetPropertyWithContext("Process PID", Layout = "${processid}"));
            }
        }

        protected override void Write(AsyncLogEventInfo info)
        {
            try
            {
                this.SendToSlack(info);
                info.Continuation(null);
            }
            catch (Exception e)
            {
                info.Continuation(e);
            }
        }

        private void SendToSlack(AsyncLogEventInfo info)
        {
            var message = RenderLogEvent(Layout, info.LogEvent);

            var slack = SlackMessageBuilder
                .Build(this._WebHookUrl)
                .OnError(e => info.Continuation(e))
                .WithMessage(message);

            if (this.ShouldIncludeProperties(info.LogEvent) || this.ContextProperties.Count > 0)
            {
                var color = this.GetSlackColorFromLogLevel(info.LogEvent.Level);
                Attachment attachment = new Attachment(info.LogEvent.Message) { Color = color };
                var allProperties = this.GetAllProperties(info.LogEvent);
                foreach (var property in allProperties)
                {
                    if (string.IsNullOrEmpty(property.Key))
                        continue;

                    var propertyValue = property.Value?.ToString();
                    if (string.IsNullOrEmpty(propertyValue))
                        continue;

                    attachment.Fields.Add(new Field(property.Key) { Value = propertyValue, Short = true });
                }
                if (attachment.Fields.Count > 0)
                    slack.AddAttachment(attachment);
            }

            var exception = info.LogEvent.Exception;
            if (!this.Compact && exception != null)
            {
                var color = this.GetSlackColorFromLogLevel(info.LogEvent.Level);
                var exceptionAttachment = new Attachment(exception.Message) { Color = color };
                exceptionAttachment.Fields.Add(new Field("StackTrace")
                {
                    Title = $"Type: {exception.GetType().ToString()}",
                    Value = exception.StackTrace ?? "N/A"
                });

                slack.AddAttachment(exceptionAttachment);
            }

            slack.Send();
        }

        private string GetSlackColorFromLogLevel(LogLevel level)
        {
            if (LogLevelSlackColorMap.TryGetValue(level, out var color))
                return color;
            else
                return "#cccccc";
        }

        private static readonly Dictionary<LogLevel, string> LogLevelSlackColorMap = new Dictionary<LogLevel, string>()
        {
            { LogLevel.Warn, "warning" },
            { LogLevel.Error, "danger" },
            { LogLevel.Fatal, "danger" },
            { LogLevel.Info, "#2a80b9" },
        };
    }
}