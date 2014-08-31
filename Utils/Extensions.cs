using System;
using System.Collections.Generic;

namespace Utils
{
	internal static class Extensions
	{
		#region String
		public static string Capitalize(this string s)
		{
			string ret = s.Trim();
			if (string.IsNullOrEmpty(ret))
				return ret;

			ret = char.ToUpper(ret[0]) + ret.Substring(1);
			return ret;
		}
		
		/// <summary>
		/// Gets the string between two characters.
		/// </summary>
		/// <returns>The returned string does not include begin and end</returns>
		public static string SubstringEx(this string s, char begin, char end,
		                                 int startIndex)
		{
			int bIndex = s.IndexOf(begin, startIndex) + 1;
			// +1: doesn't include the begin character
			int eIndex = s.IndexOf(end, bIndex);
			
			if((bIndex == -1) || (eIndex == -1))
				return string.Empty;
			
			return s.Substring(bIndex, eIndex - bIndex);
		}
		
		/// <summary>
		/// Gets the string between two characters.
		/// </summary>
		/// <returns>The returned string does not include begin and end</returns>
		public static string SubstringEx(this string s,char begin, char end)
		{
			return SubstringEx(s, begin, end, 0);
		}
		
		/// <summary>
		/// String.StartsWith + ignore case and culture
		/// </summary>
		public static bool StartsWithEx(this string s,string prefix)
		{
			s = s.Trim();
			return s.StartsWith(prefix,
			                    StringComparison.InvariantCultureIgnoreCase);
		}
		
		public static void ThrowIfNullOrEmpty(this string s, string argName)
		{
			if(s == null)
				throw new ArgumentNullException(argName);
			if(s == string.Empty)
				throw new ArgumentException(
					"Argument cannot be an empty string", argName);
		}
		
		#endregion

		
		#region Char[]
		
		public static bool Contains(this char[] array, char[] chars)
		{
			foreach(var c in chars)
			{
				if(array.Contains(c))
					return true;
			}
			
			return false;
		}
		
		public static bool Contains(this char[] array, char character)
		{
			foreach(char c in array)
			{
				if(c == character)
					return true;
			}
			
			return false;
		}
		
		#endregion

		public static void Add(this Dictionary<int, int> dic,
		                       KeyValuePair<int, int> kv)
		{
			dic.Add(kv.Key, kv.Value);
		}
	}
}
