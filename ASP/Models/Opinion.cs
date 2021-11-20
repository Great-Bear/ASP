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

        public string GetOpinionInfo(string dateCompare = null)
        {           
            var yesterday = DateTime.Now.Date.AddDays(-1);
            var today = DateTime.Now.Date;


            if (DateTime.Compare(yesterday, Moment.Date ) == 0)
            {
                return $"вчера: {Authorname} \"{Comment}\" ";
            }
            else if (DateTime.Compare(today, Moment.Date) == 0)
            {
                return $"{Moment.ToShortTimeString()}: {Authorname} \"{Comment}\" ";        
            }
            return $"{Moment} {Authorname} \"{Comment}\" ";
           
        }
    }
}
