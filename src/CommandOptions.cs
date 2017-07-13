using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace dbfaker
{
    public class CommandOptions
    {
        [Option("s", HelpText = "Connection String", Required = true)]
        public string ConnectionString { get; set; }

        [Option("c", HelpText = "Count")]
        public int Count { get; set; }

        [Option("t", HelpText = "Tables")]
        public string Tables { get; set; }

        [Option("w", HelpText = "Workers",DefaultValue = 5)]
        public int Workers { get; set; }
    }
}
