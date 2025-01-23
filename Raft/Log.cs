using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class Log
{
    public Log(int term, string command)
    {
        Term = term;
        Command = command;
    }
    public int Term { get; set; }
    public string? Command { get; set; }
}
