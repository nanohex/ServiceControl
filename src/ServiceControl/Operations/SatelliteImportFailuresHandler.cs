﻿namespace ServiceControl.Operations
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using NServiceBus;
    using NServiceBus.Faults;
    using Raven.Client;
    using ServiceBus.Management.Infrastructure.Installers;

    public class SatelliteImportFailuresHandler : IManageMessageFailures, IDisposable
    {
        public SatelliteImportFailuresHandler(IDocumentStore store, string logPath, Func<TransportMessage, object> messageBuilder)
        {
            this.store = store;
            this.logPath = logPath;
            this.messageBuilder = messageBuilder;

            Directory.CreateDirectory(logPath);
        }

        public void Dispose()
        {
            failureCircuitBreaker.Dispose();
        }

        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            Handle(e, messageBuilder(message), logPath);
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            Handle(e, messageBuilder(message), logPath);
        }

        public void FailedToReceive(Exception exception)
        {
            try
            {
                var id = Guid.NewGuid();

                var filePath = Path.Combine(logPath, id + ".txt");
                File.WriteAllText(filePath, exception.ToFriendlyString());
                WriteEvent("A message import has failed. A log file has been written to " + filePath);
            }
            finally
            {
                failureCircuitBreaker.Increment(exception);
            }
        }

        public void Init(Address address)
        {
        }

        void Handle(Exception exception, dynamic failure, string logDirectory)
        {
            try
            {
                var id = Guid.NewGuid();

                using (var session = store.OpenSession())
                {
                    failure.Id = id;

                    session.Store(failure);
                    session.SaveChanges();
                }

                var filePath = Path.Combine(logDirectory, id + ".txt");
                File.WriteAllText(filePath, exception.ToFriendlyString());
                WriteEvent("A message import has failed. A log file has been written to " + filePath);
            }
            finally
            {
                failureCircuitBreaker.Increment(exception);
            }
        }

        static void WriteEvent(string message)
        {
#if DEBUG
            new CreateEventSource().Install(null);
#endif
            EventLog.WriteEntry(CreateEventSource.SourceName, message, EventLogEntryType.Error);
        }

        readonly ImportFailureCircuitBreaker failureCircuitBreaker = new ImportFailureCircuitBreaker();
        readonly string logPath;
        readonly Func<TransportMessage, object> messageBuilder;
        readonly IDocumentStore store;
    }
}