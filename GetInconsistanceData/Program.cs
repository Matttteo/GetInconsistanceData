using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetInconsistanceData
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://protodoc.visualstudio.com/defaultcollection";
            string name = "protodoc";
            string uname = "t-yubai@microsoft.com";
            string psw = "7grcdbkdbxr2fg5y3nwlcpksqjdqelmxz2rlauuo5cr5os7yrk4q";
            Client c = new Client(url, name, uname, psw);
            c.SampleREST();
            return;
        }
    }
}
