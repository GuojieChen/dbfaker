using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace dbfaker
{
    [Alias("_fakerhistory")]
    public class FakerHistory
    {
        [StringLength(128)]
        public string Id { get; set; }

        public int Current { get; set; }

    }
}
