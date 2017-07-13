using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dbfaker
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new CommandOptions();
            if (CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
            {
                using (var service = new DbFakerService(options))
                {
                    service.Start();

                    while (!service.IsOver)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }

            Console.Read();
        }
    }
}
