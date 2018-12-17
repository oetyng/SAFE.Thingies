using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SAFE.Thingies
{
    public class Parsec
    {
        ConcurrentDictionary<Guid, DecentralizedClient> _peers = new ConcurrentDictionary<Guid, DecentralizedClient>();
        ConcurrentDictionary<long, Voting> _pendingVotings = new ConcurrentDictionary<long, Voting>();
        ConcurrentDictionary<Guid, ConcurrentQueue<EventSequence>> _newSequences = new ConcurrentDictionary<Guid, ConcurrentQueue<EventSequence>>();
        HashSet<long> _completedVotings = new HashSet<long>();
        List<EventSequence> _stableSequences = new List<EventSequence>();

        object _voteLock = new object();

        public bool Poll(Guid id, out EventSequence next)
        {
            return _newSequences[id].TryDequeue(out next);
        }

        public void VoteFor(Guid clientId, EventSequence sequence)
        {
            lock (_voteLock)
            {
                if (_completedVotings.Contains(sequence.Id))
                    return;
                if (!_pendingVotings.ContainsKey(sequence.Id))
                    _pendingVotings[sequence.Id] = new Voting { EventSequence = sequence };

                var voting = _pendingVotings[sequence.Id];

                if (voting.Votes.Contains(clientId))
                {
                    Console.WriteLine($"Double vote: {clientId}.");
                    return;
                }

                voting.Votes.Add(clientId);

                if (IsQuorum(voting))
                {
                    _peers.Keys
                        .ToList()
                        .ForEach(c => _newSequences[c].Enqueue(sequence));
                    _stableSequences.Add(sequence);
                    _pendingVotings.TryRemove(voting.EventSequence.Id, out voting);
                    _completedVotings.Add(voting.EventSequence.Id);
                }
            }
        }

        internal void TryAdd(DecentralizedClient client)
        {
            _peers[client.Id] = client;
            _newSequences[client.Id] = new ConcurrentQueue<EventSequence>(_stableSequences);
        }

        bool IsQuorum(Voting voting)
        {
            return voting.Votes.Count > _peers.Count / 2;
        }

        internal void RequestChange(Cmd cmd)
        {
            _peers.Values
                .ToList()
                .ForEach(c => c.ReceiveChange(cmd));
        }
    }

    public class Voting
    {
        public HashSet<Guid> Votes { get; } = new HashSet<Guid>();
        public EventSequence EventSequence { get; set; }
    }
}
