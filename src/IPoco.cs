using System.Collections.Generic;

namespace dbfaker
{
    public interface IPoco
    {
        IList<DbTable> GetDbTables();
    }
}
