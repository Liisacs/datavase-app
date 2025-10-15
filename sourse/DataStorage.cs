using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class DataStorage
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    public string GetConnectionString()
        {
            return $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};Convert Zero Datetime=True;Allow Zero Datetime=True;";
        }

    }
}