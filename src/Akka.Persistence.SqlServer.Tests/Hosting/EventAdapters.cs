using Akka.Persistence.Journal;

namespace Akka.Persistence.SqlServer.Tests.Hosting;

public class EventAdapters
{
    public sealed class Event1{ }
    public sealed class Event2{ }

    public sealed class EventMapper1 : IWriteEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            return evt;
        }
    }

    public sealed class Tagger : IWriteEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            if (evt is Tagged t)
                return t;
            return new Tagged(evt, new[] { "foo" });
        }
    }

    public sealed class ReadAdapter : IReadEventAdapter
    {
        public IEventSequence FromJournal(object evt, string manifest)
        {
            return new SingleEventSequence(evt);
        }
    }

    public sealed class ComboAdapter : IEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            return evt;
        }

        public IEventSequence FromJournal(object evt, string manifest)
        {
            return new SingleEventSequence(evt);
        }
    }
}