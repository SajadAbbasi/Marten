using Es.Framework;
using Marten;
using Marten.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Es.EventStore.Marten
{
    public class MartenEventStore : Framework.IEventStore
    {
        private readonly DocumentStore documentStore;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore
        };
        public MartenEventStore()
        {
            documentStore = DocumentStore.For("Username=postgres;Password=postgres;Host=localhost;Port=5432;Database=postgres;");
            //documentStore.Options.Events.StreamIdentity = StreamIdentity.AsString;
            //documentStore.Advanced.Clean.CompletelyRemoveAll();
        }

        public Task<IReadOnlyCollection<EventStoreItem>> GetAll(DateTime? afterDateTime)
        {
            using (IDocumentSession session = documentStore.LightweightSession())
            {
                var events = session.Events.QueryAllRawEvents().Where(x => x.Timestamp > afterDateTime).ToList();
                IReadOnlyCollection<EventStoreItem> result = events.Select(x =>
                new EventStoreItem
                {
                    CreatedAt = x.Timestamp.DateTime,
                    Id = x.Id,
                    Data = JsonConvert.SerializeObject(x.Data, _jsonSerializerSettings),
                    Sequence = (int)x.Sequence,
                    Version = x.Version,
                    AggregateId = x.StreamId.ToString(),
                    Aggregate = x.StreamKey,
                    Name = x.Data.GetType().Name,
                }).ToList().AsReadOnly();
                return Task.FromResult(result);
            }
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> LoadAsync(Guid aggregateRootId, string aggregateName)
        {
            //string streamName = GetStreamName(aggregateRootId, aggregateName);

            using (IDocumentSession session = documentStore.LightweightSession())
            {
                IReadOnlyList<IEvent> events = await session.Events.FetchStreamAsync(aggregateRootId);
                return events.Select(x => x.Data as IDomainEvent).ToList().AsReadOnly();
            }
        }

        public async Task SaveAsync(Guid aggregateId, string aggregateName, int originatingVersion, IReadOnlyCollection<IDomainEvent> events)
        {
            //string streamName = GetStreamName(aggregateId, aggregateName);

            using (IDocumentSession session = documentStore.LightweightSession())
            {
                session.Events.Append(aggregateId, ++originatingVersion, events);
                await session.SaveChangesAsync();
            }
        }

        private static string GetStreamName(Guid aggregateId, string aggregateName)
        {
            return $"{aggregateName}-{aggregateId}";
        }
    }
}
