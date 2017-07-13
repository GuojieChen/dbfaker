using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace dbfaker
{
    public interface IFakerDirver
    {
        OrmLiteConnectionFactory ConnectionFactory { get; }

        IPoco Poco { get; }
    }
}
