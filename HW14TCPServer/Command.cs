using System;
using System.Collections.Generic;
using System.Text;

namespace HW14TCPServer
{
    public class Command
    {
        public string key;
        public string value;
        public Type type;

        public Command(string key, string value, Type type)
        {
            this.key = key;
            this.value = value;
            this.type = type;
        }

        public enum Type
        {
            SET, 
            GET, 
            DELETE, 
            STA
        }
    }
}
