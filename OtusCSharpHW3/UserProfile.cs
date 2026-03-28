using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtusCSharpModels
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime CreatedOn { get; set; }

        public UserProfile(string Username) 
        { 
            this.Username = Username;
        }
    }
}
