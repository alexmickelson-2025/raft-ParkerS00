using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class Log
{
    public Log(int term, string key, string value)
    {
        Term = term;
        Key = key;
        Value = value;
    }
    public int Term { get; set; }
    public string? Key { get; set; }
    public string Value { get; set; }
}
