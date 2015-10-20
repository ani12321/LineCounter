using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineCounter
{
    public static class Log
    {
        public static void Add(string message)
        {
            message = DateTime.Now + Environment.NewLine + message + Environment.NewLine;
            File.AppendAllText(Constants.LogFile,message);

        }
    }
}
