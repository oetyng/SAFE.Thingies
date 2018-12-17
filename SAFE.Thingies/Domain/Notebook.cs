using System;
using System.Collections.Generic;

namespace SAFE.Thingies.Domain
{
    internal class Notebook : Aggregate
    {
        public NotebookState State { get; } = new NotebookState();

        List<Event> Handle(InitiateNotebook cmd)
        {
            if (_id.HasValue && _id.Value == cmd.StreamId)
                return new List<Event>();
            else if (_id.HasValue)
                throw new InvalidOperationException();

            return new List<Event> { new NotebookInitiated(cmd.StreamId) };
        }

        List<Event> Handle(AddNote cmd)
        {
            if (State.AddedNotes.Contains(cmd.Note))
                return new List<Event>();

            return new List<Event> { new NoteAdded(cmd.Note) };
        }

        void Apply(NoteAdded e)
        {
            State.AddedNotes.Add(e.Note);
        }

        void Apply(NotebookInitiated e)
        {
            _id = e.StreamId;
        }
    }

    internal class NotebookState
    {
        public HashSet<string> AddedNotes { get; set; } = new HashSet<string>();
    }

    public class NotebookInitiated : Event
    {
        public NotebookInitiated(Guid streamId)
        {
            StreamId = streamId;
        }

        public Guid StreamId { get; }
    }

    public class InitiateNotebook : Cmd
    {
        public InitiateNotebook(Guid targetId, Guid streamId)
            : base(targetId)
        {
            StreamId = streamId;
        }

        public Guid StreamId { get; }
    }

    public class NoteAdded : Event
    {
        public NoteAdded(string note)
        {
            Note = note;
        }

        public string Note { get; }
    }

    public class AddNote : Cmd
    {
        public AddNote(Guid targetId, string note)
            : base(targetId)
        {
            Note = note;
        }

        public string Note { get; }
    }
}
