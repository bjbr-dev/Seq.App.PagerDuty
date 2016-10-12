// <copyright file="PagerDutyReactor.cs" company="Berkeleybross">
//     Copyright (c) Berkeleybross. All rights reserved.
// </copyright>
namespace Seq.App.PagerDuty
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PagerDutyAPI;
    using Seq.Apps;
    using Seq.Apps.LogEvents;

    [SeqApp("PagerDuty")]
    public class PagerDutyReactor
        : Reactor, ISubscribeTo<LogEventData>
    {
        private const string DefaultApplicationName = "Seq";

        private const string ApplicationNameHelp =
            "The name this app should use when creating an incident in PagerDuty. Defaults to " + DefaultApplicationName;

        private IntegrationAPI client;
        private List<string> additionalProperties;

        [SeqAppSetting(DisplayName = "ServiceKey", InputType = SettingInputType.Password)]
        public string ServiceKey { get; set; }

        [SeqAppSetting(DisplayName = "Application Name", IsOptional = true, HelpText = ApplicationNameHelp)]
        public string ApplicationName { get; set; }

        [SeqAppSetting(
             DisplayName = "Application URL",
             IsOptional = true,
             HelpText = "The URL this app should use when creating an incident in PagerDuty. Defaults to empty.")]
        public string ApplicationUrl { get; set; }

        [SeqAppSetting(
             DisplayName = "Incident Id Property Name",
             IsOptional = true,
             HelpText = "The name of the property in the event being logged to send to PagerDuty as the incident Id.")]
        public string IncidentIdPropertyName { get; set; }

        [SeqAppSetting(
             DisplayName = "Additional Property Names",
             IsOptional = true,
             HelpText = "The names of additional event properties to include in the PagerDuty incident. One per line.",
             InputType = SettingInputType.LongText)]
        public string AdditionalProperties { get; set; }

        public void On(Event<LogEventData> evt)
        {
            if (this.client == null)
            {
                return;
            }

            var data = new Dictionary<string, string>();
            foreach (var propertyName in this.additionalProperties)
            {
                var value = GetPropertyOrDefault(evt, propertyName);
                if (value != null)
                {
                    data[propertyName] = value.ToString();
                }
            }

            var incidentId = GetPropertyOrDefault(evt, this.IncidentIdPropertyName);
            var result = this.client.Trigger(evt.Data.RenderedMessage, data, incidentId?.ToString());
            if (!result.IsSuccess())
            {
                this.Log.Error("Error sending event {EventId} to PagerDuty: {Message}", evt.Id, result.Message);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.client = IntegrationAPI.MakeClient(
                new APIClientInfo(this.ApplicationName ?? DefaultApplicationName, this.ApplicationUrl),
                this.ServiceKey,
                new Retry(TimeSpan.FromSeconds(1), 1));

            this.additionalProperties = SplitOnNewLine(this.AdditionalProperties).ToList();
        }

        private static object GetPropertyOrDefault(Event<LogEventData> evt, string name)
        {
            object value;
            return string.IsNullOrEmpty(name) ||
                   !evt.Data.Properties.TryGetValue(name, out value)
                ? null
                : value;
        }

        private static IEnumerable<string> SplitOnNewLine(string addtionalProperties)
        {
            if (addtionalProperties == null)
            {
                yield break;
            }

            using (var reader = new StringReader(addtionalProperties))
            {
                yield return reader.ReadLine();
            }
        }
    }
}