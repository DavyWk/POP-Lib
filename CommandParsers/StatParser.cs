using System.Collections.Generic;

using POP;

namespace POP.CommandParsers
{
	internal static class StatParser
	{
		public static KeyValuePair<int, int> Parse(string s)
		{
			if(!Protocol.CheckHeader(s))
				return new KeyValuePair<int, int>(-1, -1);
			
			s = Protocol.RemoveHeader(s);
			string[] splitted = s.Split(' ');
			int nb = int.Parse(splitted[0]);
			int size = int.Parse(splitted[1]);
			
			return new KeyValuePair<int, int>(nb, size);
		}
	}
}