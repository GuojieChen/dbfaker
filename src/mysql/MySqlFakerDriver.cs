using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace dbfaker.mysql
{
    public class MySqlFakerDriver:IFakerDirver
    {
        public OrmLiteConnectionFactory ConnectionFactory { get; }
        public IPoco Poco { get; }

        public MySqlFakerDriver(string connectionstring)
        {
            ConnectionFactory = new OrmLiteConnectionFactory(connectionstring, MySqlDialect.Provider);
            Poco = new MySqlPoco();
        }
    }
}
