using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public class TimestampHelper
    {

        public static string Now()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }
}
