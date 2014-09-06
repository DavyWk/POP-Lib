using System;
using System.Collections.Generic;

using Utils;

namespace POP.CommandParsers
{
    internal static class ListParser
    {
        public static Dictionary<int, int> Parse(List<string> messageList)
        {
            var dic = new Dictionary<int, int>();
            foreach(var s in messageList)
            	dic.Add(Parse(s));
            
            return dic;
        }
        
        public static KeyValuePair<int, int> Parse(string s)
        {
        	// ID sizeInBytes
        	
        	string[] elements = s.Split(' ');
        	int id = 0;
        	int size = -1;
        	
        	int.TryParse(elements[0], out id);
        	int.TryParse(elements[1], out size);
        	
        	return new KeyValuePair<int, int>(id, size);
        }
    }
}
