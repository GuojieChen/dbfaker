using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbfaker.mysql
{
    public class MySqlPoco : IPoco
    {
        public IList<DbTable> GetDbTables()
        {
            throw new NotImplementedException();
        }
    }
}
