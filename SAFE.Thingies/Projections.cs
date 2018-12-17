
namespace SAFE.Thingies
{
    public class Projections
    {
        // projection local store (and the network store?)

        public QueryResult Query(Query query)
        {
            return new QueryResult();
        }

        public void Build(EventData e)
        {
            // build models in store
        }
    }
}
