using System.Collections.Generic;

using POP;

namespace POP.CommandParsers
{
	public static class UniqueIdentifierParser
	{
		public static string Parse(string s)
		{
			return InternalParse(s).Value;
		}
		
		public static Dictionary<int, string> Parse(List<string> list)
		{
			var dic = new Dictionary<int, string>();
			
			foreach(var s in list)
			{
				var kv = InternalParse(s);
				dic.Add(kv.Key, kv.Value);
			}
			
			return dic;
		}
		
		private static KeyValuePair<int, string> InternalParse(string s)
		{
			s = s.Trim();
			s = Protocol.RemoveHeader(s);
			var element = s.Split(' ');
			
			string uid = element[1];
			int nb = int.Parse(element[0]);
			
			return new KeyValuePair<int, string>(nb, uid);
		}
	}
}