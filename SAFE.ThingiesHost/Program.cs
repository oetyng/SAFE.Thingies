using SAFE.Thingies;
using SAFE.Thingies.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.ThingiesHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsec = new Parsec();

            var clients = Enumerable.Range(0, 12)
                .Select(c => new DecentralizedClient(parsec))
                .ToList();

            clients.ForEach(c => c.Run(CancellationToken.None));

            var currentClient = clients.First();

            currentClient.InitiateChange(new InitiateNotebook(Guid.NewGuid(), Guid.Empty));
            Task.Delay(200).ConfigureAwait(false).GetAwaiter().GetResult();
            currentClient.InitiateChange(new AddNote(Guid.NewGuid(), "note"));

            Console.ReadKey();
        }
    }
}
