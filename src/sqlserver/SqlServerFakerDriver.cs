using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace dbfaker.sqlserver
{
    public class SqlServerFakerDriver:IFakerDirver
    {
        public OrmLiteConnectionFactory ConnectionFactory { get; }
        public IPoco Poco { get; }

        public SqlServerFakerDriver(string connectionstring)
        {
            ConnectionFactory = new OrmLiteConnectionFactory(connectionstring, SqlServerDialect.Provider);
            Poco = new SqlServerPoco(connectionstring);
        }
    }
}
