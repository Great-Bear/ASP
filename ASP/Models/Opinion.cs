using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevOpsBasics.Models
{
    public class Opinion
    {
        public String Usernik { get; set; }

        public String Comment { get; set; }

        public String Authorname { get; set; }

        public DateTime Moment { get; set; } 
    }
}
