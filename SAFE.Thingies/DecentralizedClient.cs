using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.Thingies
{
    // Partitioning based on streamKey (i.e. [streamName]@[streamId])
    // This way, all streams are evenly distributed in the network.
    // A cmd is routed by the target streamkey, to the correct section.
    // Projections must be stored in all sections though? (Since they require events from all sorts of streams.)
    public class DecentralizedClient
    {
        Parsec _parsec;
        EventDb _db = new EventDb();
        EventRouter _eventRouter = new EventRouter();
        Projections _projections = new Projections();

        public Guid Id { get; } = Guid.NewGuid();

        public DecentralizedClient(Parsec parsec)
        {
            _parsec = parsec;
        }

        public async Task Run(CancellationToken cancellation)
        {
            _parsec.TryAdd(this);
            while (!cancellation.IsCancellationRequested)
            {
                if (!_parsec.Poll(Id, out EventSequence nextEvents))
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    continue;
                }

                Apply(nextEvents);
                var resultingCmds = _eventRouter.GetResultingCmds(nextEvents);

                foreach (var cmd in resultingCmds)
                    InitiateChange(cmd);
            }
        }

        // entire app runs on the client
        // so when user wants to do something, 
        // it happens locally first.
        public void InitiateChange(Cmd cmd)
        {
            var ar = GetAggregate(cmd);
            var events = ar.Handle(cmd);
            if (events.Count > 0)
            {
                _parsec.RequestChange(cmd); // but not all cmds are handled within the group of this peer (?) (otherwise, there is no sharding at all) 
                var sequence = new EventSequence(events); // So parsec must route this cmd to another section if the recipient stream is not handled within this section.
                _parsec.VoteFor(Id, sequence); // not only do we ask all others to do the same calculations, we also vote for the outcome.
            }
        }

        // received when another peer has initiated a change
        internal void ReceiveChange(Cmd cmd)
        {
            var ar = GetAggregate(cmd);
            var events = ar.Handle(cmd);
            if (events.Count > 0)
            {
                var sequence = new EventSequence(events);
                _parsec.VoteFor(Id, sequence); // this is the outcome we vote for
            }
        }

        // quite unclear how this would actually work, since projections are by definition built by any streams, and so could require events from other sections.
        public QueryResult Query(Query query)
        {
            return _projections.Query(query); // this probably needs some P2P network querying structure.
        }

        // This is probably only the blocks decided on in this particular group.
        void Apply(EventSequence sequence)
        {
            foreach (var e in sequence.Events)
            {
                var ar = GetAggregate(e.StreamName, e.StreamId);
                ar.ApplyEvent(e);
                _projections.Build(e); // <= this is dubious. We need to build projections from all events of all streams. How do we get those blocks?
                Console.WriteLine(e.Payload);
            }
            _db.Add(sequence); // needs to be an event store, that can load streams efficiently
        }

        Aggregate GetAggregate(Cmd cmd)
        {
            var (streamName, streamId) = GetRecipient(cmd);
            return GetAggregate(streamName, streamId);        }

        Aggregate GetAggregate(string streamName, Guid streamId)
        {
            var stream = _db.Load(streamName, streamId);
            var ar = new Domain.Notebook();
            ar.ApplyEvents(stream);
            return ar;
        }

        (string streamName, Guid streamId) GetRecipient(Cmd cmd)
        {
            // a cmd type can only ever go to a specific aggregate type (i.e. stream name)
            // and a cmd always has a single recipient (i.e. stream id)
            return ("Notebook", Guid.Empty);
        }
    }
}
