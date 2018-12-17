using System.Collections.Generic;
using System.Linq;

namespace SAFE.Thingies
{
    public class EventRouter
    {
        Dictionary<string, List<ProcessManager>> _eventSubscriptions = new Dictionary<string, List<ProcessManager>>();

        // This kind of presumes and requires that there is 
        // a fixed order in which decisions are made in response to an event, 
        // since the cmds will be sent in the order the processmanagers are placed in the lists.
        public List<Cmd> GetResultingCmds(EventSequence sequence)
        {
            var cmds = new List<Cmd>();

            foreach (var e in sequence.Events)
            {
                if (!_eventSubscriptions.ContainsKey(e.EventName))
                    return new List<Cmd>();

                var nextCmds = _eventSubscriptions[e.EventName]
                    .Select(c => c.DecideNextCmd(e));

                cmds.AddRange(nextCmds);
            }

            return cmds;
        }
    }
}
