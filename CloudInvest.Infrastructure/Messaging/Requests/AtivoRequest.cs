using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvest.Infrastructure.Messaging.Requests
{
    public class AtivoRequest
    {
        public string Ticker { get; set; }
        public decimal Preco { get; set; }
    }
}
