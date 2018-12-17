using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAFE.Thingies
{
    public abstract class Aggregate
    {
        Dictionary<Type, Func<Cmd, List<Event>>> _cmdHandlers = new Dictionary<Type, Func<Cmd, List<Event>>>();
        Dictionary<Type, Action<Event>> _eventHandlers = new Dictionary<Type, Action<Event>>();

        protected Guid? _id;

        public int Version { get; private set; } = -1;
        public string StreamName { get; private set; }
        public Guid StreamId => _id.HasValue ? _id.Value : Guid.Empty;
        public string StreamKey => $"{StreamName}@{StreamId}";
        public readonly List<string> Cmds = new List<string>();

        public Aggregate()
        {
            var t = this.GetType();
            StreamName = t.Name;

            var applyMethods = GetAllMethods(t)
                .Where(m => m.Name == "Apply");
            foreach (var m in applyMethods)
                _eventHandlers[m.GetParameters().First().ParameterType] = new Action<Event>((e) => m.Invoke(this, new object[] { e }));

            var handleMethods = GetAllMethods(t)
                .Where(m => m.Name == "Handle");
            foreach (var m in handleMethods)
                _cmdHandlers[m.GetParameters().First().ParameterType] = new Func<Cmd, List<Event>>((e) => (List<Event>)m.Invoke(this, new object[] { e }));

            Cmds = _cmdHandlers.Keys.Select(c => c.Name).ToList();
        }

        public List<EventData> Handle(Cmd cmd)
        {
            return _cmdHandlers[cmd.GetType()](cmd)
                .Select(@event => new EventData(@event, StreamName, StreamId, Version + 1))
                .ToList();
        }

        internal void ApplyEvents(IEnumerable<EventData> events)
        {
            foreach (var e in events)
                ApplyEvent(e);
        }

        internal void ApplyEvent(EventData @event)
        {
            if (@event.SequenceNr != Version + 1)
                throw new InvalidOperationException("Version");

            Apply(@event.GetEvent());
            Version++;
        }

        void Apply(Event @event)
        {
            _eventHandlers[@event.GetType()](@event);
        }

        // If have yet another base class with Apply methods too..
        IEnumerable<MethodInfo> GetAllMethods(Type t) // recursive
        {
            if (t == null)
                return Enumerable.Empty<MethodInfo>();

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return t.GetMethods(flags).Concat(GetAllMethods(t.BaseType));
        }
    }
}
