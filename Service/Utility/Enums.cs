using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Utility
{
    public enum RequestStatus
    {
        New = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }
}
