
namespace SAFE.Thingies
{
    // The process manager is an advanced state machine.
    // It is triggered by system events, queries the system state
    // and then decides what should happen next.
    public abstract class ProcessManager
    {
        Projections _projections;

        public ProcessManager(Projections projections)
        {
            _projections = projections;
        }

        public Cmd DecideNextCmd(EventData e)
        {
            var result = _projections.Query(new Query());
            if (result.Something)
                return default(Cmd);
            else
                return default(Cmd);
        }
    }
}
