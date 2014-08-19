using System;
using System.IO;
using System.Text;
using System.Security;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Utils
{
	public static class Extensions
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
		
		public static string CleanPath(this string s)
		{
			//TODO: StringBuilder
			int index = 0;
			char[] invalidChars = Path.GetInvalidFileNameChars();
			char[] chars = s.ToCharArray();
			var sb = new StringBuilder(s);
			
			foreach(char c in chars)
			{
				if(invalidChars.Contains(c))
				{
					index = sb.IndexOf(c, index);
					sb = sb.Remove(index, 1);
				}
			}
			
			return s;
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
		
		public static bool Contains(this string[] array, string search,
		                            bool ignoreCase = false)
		{
			string original = search;
			foreach(var s in array)
			{
				string temp = s;
				if(ignoreCase)
				{
					temp = temp.ToLower();
					search = search.ToLower();
				}
				
				if(s == search)
					return true;
				
				search = original;
			}
			
			return false;
		}
		#endregion
		
		#region List<string>
		
		public static string ToString(this List<string> list, string separator)
		{
			return string.Join(separator, list.ToArray());
		}
		
		public static void Add(this List<string> list, string format,
		                       params object[] args)
		{
			list.Add(string.Format(format, args));
		}
		#endregion
		
		public static int IndexOf(this StringBuilder sb, char c,
		                          int startIndex = 0)
		{
			for(int i = startIndex; i < sb.Length; i++)
			{
				if(sb[i] == c)
					return i;
			}
			
			return -1;
		}

		public static void Add(this Dictionary<int, int> dic,
		                       KeyValuePair<int, int> kv)
		{
			dic.Add(kv.Key, kv.Value);
		}

		
		public static int IndexOf(this string[] array, string search,
		                          bool ignoreCase = false)
		{
			if(ignoreCase)
				search = search.ToLower();
			for(int i = 0; i < array.Length; i++)
			{
				string temp = array[i];
				if(ignoreCase)
					temp.ToLower();
				
				if(temp == search)
					return i;
			}
			
			return -1;
		}
		
		/// <summary>
		/// Gets a System.String form a SecureString
		/// </summary>
		/// <returns>The original string</returns>
		public static string ToAsciiString(this SecureString s)
		{ // it doesnt't secure the string but its easier to handle password input in a SecureString
			string ret;
			IntPtr pointer = Marshal.SecureStringToBSTR(s);
			ret = Marshal.PtrToStringBSTR(pointer);
			Marshal.ZeroFreeBSTR(pointer);

			return ret;
		}

	}
}
