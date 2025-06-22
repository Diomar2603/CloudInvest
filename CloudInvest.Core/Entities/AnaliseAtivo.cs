using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvest.Core.Entities
{
    public class AnaliseAtivo
    {
        public int Id { get; set; } 
        public string Ticker { get; set; } 
        public decimal PrecoAnalisado { get; set; }
        public string Recomendacao { get; set; }
        public DateTime DataAnalise { get; set; }
    }
}
