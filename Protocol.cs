using System.Text;

namespace POP
{
	public static class Protocol
	{
		public static bool CheckHeader(string s)
		{
			if(s.TrimStart().StartsWith(Constants.OK))
				return true;
			else
				return false;
		}
		
		/// <summary>
		/// Removes the "+OK"/"-ERR" header.
		/// </summary>
		/// <remarks>Also removes the trailling space.</remarks>
		public static string RemoveHeader(string s)
		{
			s = s.Trim();
			
			if(CheckHeader(s))
				s = s.Replace("+OK", string.Empty);
			else
				s = s.Replace("-ERR", string.Empty);
			
			s = s.TrimStart();
			
			return s;
		}
		
		public static string AddHeader(bool flag, string s)
		{
			var sb = new StringBuilder(s);
			var header =  flag ? "+OK" : "-ERR";
			
			sb.Insert(0, " ", 1);
			sb.Insert(0, header, 1);
			
			return sb.ToString();
		}
	}
}
