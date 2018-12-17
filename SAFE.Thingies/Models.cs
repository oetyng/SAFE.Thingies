using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAFE.Thingies
{
    public abstract class Cmd
    {
        public Cmd(Guid targetId)
        {
            TargetId = targetId;
        }

        public Guid TargetId { get; }
    }

    public abstract class Event
    { }

    public class EventData
    {
        public string Id { get; }
        public string EventName { get; }
        public string Payload { get; }
        public string StreamName { get; }
        public Guid StreamId { get; }
        public DateTime TimeStamp { get; }
        public int SequenceNr { get; }
        public string StreamKey => $"{StreamName}@{StreamId}";

        public EventData(Event @event, string streamName, Guid streamId, int sequenceNr)
        {
            var eventName = @event.GetType().Name;
            var payload = JsonConvert.SerializeObject(@event);

            Id = $"{sequenceNr}@{eventName}/{streamName}@{streamId}/{payload}"; // this is what parsec should agree on
            EventName = eventName;
            Payload = payload;
            SequenceNr = sequenceNr;
            StreamName = streamName;
            StreamId = streamId;
            TimeStamp = DateTime.UtcNow;
        }

        public Event GetEvent()
        {
            return (Event)JsonConvert.DeserializeObject(Payload, Assembly.GetExecutingAssembly().GetTypes().First(c => c.Name == EventName));
        }
    }

    public class EventSequence
    {
        public long Id { get; }
        public List<EventData> Events { get; }

        public EventSequence(List<EventData> events)
        {
            var joined = events.Select(c => 
                c.Id
                .GetHashCode()
                .ToString()
                .Replace("-", string.Empty));

            Id = long.Parse(string.Join(string.Empty, joined)); // very simple
            Events = events;
        }
    }

    public class Query
    { }

    public class QueryResult
    {
        public bool Something { get; set; }
    }

    // needs to be an event store, that can index and load streams
    public class EventDb
    {
        ConcurrentDictionary<string, List<EventData>> _events = new ConcurrentDictionary<string, List<EventData>>();

        public void Add(EventSequence sequence)
        {
            var streams = sequence.Events.GroupBy(c => c.StreamKey);
            foreach (var stream in streams)
            {
                if (!_events.ContainsKey(stream.Key))
                    _events[stream.Key] = new List<EventData>();
                _events[stream.Key].AddRange(stream);
            }
        }

        public List<EventData> Load(string streamName, Guid streamId)
        {
            // in reality, this would be an indexed access
            var streamKey = $"{streamName}@{streamId}";

            if (!_events.ContainsKey(streamKey))
                return new List<EventData>();

            return _events[streamKey];
        }
    }
}
