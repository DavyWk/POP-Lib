using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

using Utils;

namespace POP
{
	//TODO: Support MIME format.
	
	public class MailParser
	{
		public POPMessage Message { get; private set; }
		
		private List<string> lines;
		
		
		public MailParser(List<string> messageLines)
		{
			lines = messageLines;
			POPMessage m = new POPMessage();
			m.Raw = lines;
			
			foreach(var l in lines)
			{
				// Just in case.
				var trimmed = l.Trim();
				var lowered = trimmed.ToLower();
				
				if(lowered.StartsWith("message-id:"))
					m.ID = GetID(trimmed);
				else if(lowered.StartsWith("from:"))
					m.Sender = GetSender(trimmed);
				else if(lowered.StartsWith("to:") && (m.Receivers == null))
					m.Receivers = GetReceivers(trimmed);
				else if(lowered.StartsWith("subject:"))
					m.Subject = GetSubject(trimmed);
				// Date MUST be in lowercase
				else if(lowered.StartsWith("date:"))
					m.ArrivalTime = GetDate(lowered);
				else if(lowered.StartsWith("content-type:"))
					m.CharSet = GetEncoding(trimmed);
				else if(string.IsNullOrWhiteSpace(trimmed))
				{
					int currentLine = lines.IndexOf(l);
					m.Header = lines.GetRange(0, currentLine);
					break;
				}
			}
						
			// Some SMTP sever don't send all the fields.
			m.Subject = MailParsingUtils.CompleteSubject(m.Subject);
			if(m.CharSet == null)
				m.CharSet = Encoding.UTF8;
			if(m.Receivers == null)
			{
				m.Receivers = new List<Person>();
				m.Receivers.Add(new Person("ERROR", "ERROR"));
			}
			
			m.ContainsHTML = CheckForHTML();
			m.Body = GetBody(m.CharSet);
			
			Message = m;
		}
		
		
		private static string GetID(string s)
		{
			return s.SubstringEx('<', '>');
		}
		
		private Person GetSender(string s)
		{
			Person p = new Person();
			// In case there's something interesting on
			// the following line.
			int offset = lines.IndexOf(s); // "From:"
			if(offset == -1)
				offset = lines.IndexOf(string.Concat(s, " ")); // "Fron: "
			
			string nextLine = lines[offset + 1];
			int index = -1;
			s = s.Replace("From:", string.Empty).Trim();
			s = MailDecoder.RemoveEncoding(s);
			
			if(string.IsNullOrWhiteSpace(s))
			{ // Means that sender info is on the other line.
				return GetSender(nextLine);
			}
			
			if(s.IndexOf('"') > -1)
				p.Name = s.SubstringEx('"', '"');
			else
			{
				index = s.IndexOf('<');
				if(index > 0)
				{
					p.Name = s.Substring(0, index - 1);
					p.Name = p.Name.Trim();
				}
				else
					index = 0;
			}
			p.Name = p.Name.Replace('_', ' ');

			// In case the next line contains the email address.
			if(nextLine.StartsWith("\t")
			   || nextLine.StartsWith(" ")
			   || nextLine.StartsWith("  "))
			{
				nextLine = nextLine.Replace('\t', ' ');
				nextLine = nextLine.Trim();
				// If next line contains a valid email address.
				if(nextLine.StartsWith("<")
				   && nextLine.Contains("@")
				   && nextLine.EndsWith(">"))
				{
					p.EMailAddress = nextLine.SubstringEx('<', '>');
				}
			}
			else
				p.EMailAddress = s.SubstringEx('<', '>');
			
			if(string.IsNullOrWhiteSpace(p.EMailAddress))
				p.EMailAddress = s.Substring(index, s.Length - index);
			
			// Just because it looks better.
			p.EMailAddress = p.EMailAddress.ToLower();
			
			return p;
		}
		
		private List<Person> GetReceivers(string s)
		{
			int offset = lines.IndexOf(s);
			int index = s.IndexOf(':');
			int lastIndex = 0;
			var receivers = new List<Person>();
			
			// In case there is nothing after the "To:".
			if((index + 1) == s.Length)
				return null;
			// +2: Also remove the space.
			s = s.Remove(0, index + 2);
			
			// Handles multiple receivers.
			var nextLine = lines[++offset];
			int extraChars = MailDecoder.StartsWith(nextLine);
			while(extraChars > 0)
			{
				nextLine = nextLine.Remove(0, extraChars);
				s = string.Format("{0} {1}", s, nextLine);
				
				nextLine = lines[++offset];
				extraChars = MailDecoder.StartsWith(nextLine);
			}
			
			s = MailDecoder.RemoveEncoding(s);
			var delimitor = new char[] { '"', ' ', '<'};
			
			do
			{
				lastIndex = index;

				if((index = s.IndexOfAny(delimitor, index)) > -1)
				{
					Person receiver = new Person();
					receiver.Name = s.SubstringEx('"', '"', index);
					
					if(receiver.Name  != string.Empty)
					{
						// Just to be sure it gets the correct string.
						if(index > s.IndexOf(',', lastIndex))
							receiver.Name = string.Empty;
					}
					
					if(receiver.Name == string.Empty)
						receiver.Name = s.SubstringEx(' ', '<', index).Trim();
					
					receiver.EMailAddress = s.SubstringEx('<', '>', index);
					if(receiver.EMailAddress == string.Empty)
					{
						int nextColon = s.IndexOf(',', index);
						if(nextColon == -1)
							nextColon = s.Length;
						receiver.EMailAddress = s.Substring(index,
						                                    nextColon - index)
							.Trim();
					}

					
					// In case the name is the same as the email address.
					if(receiver.Name == receiver.EMailAddress)
						receiver.Name = string.Empty;
					
					// Just to handle the case where the address is between
					// parenthesis.
					if(receiver.Name.SubstringEx('"', '"')
					   == receiver.EMailAddress)
					{
						receiver.Name = string.Empty;
					}
					
					receivers.Add(receiver);
				}
				else
				{
					index = s.IndexOfAny(new char[] { ' ', ','}, lastIndex);
					
					
					if((index == -1) || (lastIndex == index))
					{
						//index = s.Length;
						receivers.Add(new Person(string.Empty, s));
						break;
					}
					
					string address  = s.Substring(index + 1);
					receivers.Add(new Person(string.Empty,s));
				}
			}
			while ((index = s.IndexOf(',', index)) > 0);
			
			// Sometimes, addresses are in uppercase.
			for(int i = 0; i < receivers.Count; i++)
				receivers[i].EMailAddress = receivers[i].EMailAddress.ToLower();
			
			
			return receivers;
		}
		
		private string GetSubject(string s)
		{
			int offset = lines.IndexOf(s);
			// Skip space.
			int index = s.IndexOf(':') + 2;
			var ret = string.Empty;
			
			if((index != -1) && (index < s.Length))
			{
				s = s.Substring(index, s.Length - index);
				s = MailDecoder.RemoveEncoding(s);
				ret = s;
			}
			
			string nextLine = lines[offset+1];
			if(nextLine.StartsWith("\t") || nextLine.StartsWith(" "))
			{
				nextLine = nextLine.Replace(" ", string.Empty);
				nextLine = nextLine.Replace("\t", string.Empty);
				nextLine = MailDecoder.RemoveEncoding(nextLine);
				ret = string.Concat(ret, nextLine);
			}
			// Some subjects are formatted like that ...
			ret = ret.Replace('_', ' ');
			ret = MailParsingUtils.CompleteSubject(ret);
			
			return ret.Trim();
		}
	
		[Obsolete("Not accurate. Use DateTime.Parse istead.")]
		private static DateTime ParseDate(string s)
		{
			s = MailDecoder.DecodeSpecialChars(s);
			string dateFormat = "ddd dd MMM yyyy HH:mm:ss";
			int index = s.IndexOf(':') + 1;
			string date = s.Substring(index, s.Length - index);
			
			// If there is a double space, remove one space.
			if(date.IndexOf("  ") > 0)
				date = date.Remove(date.IndexOf("  "), 1);
			

			date = date.Replace(",", string.Empty).Trim();
			
			// Special parenthesis like  02:31:57 +0000 (GMT+00:00)
			index = date.LastIndexOf('(');
			if(index == -1)
			{
				char[] delimitor = new char[]  {'-','+',' '};
				index = date.LastIndexOfAny(delimitor);
			}
			
			int lastSpace = date.LastIndexOf(' ') - 1;
			if((lastSpace == -2) || (lastSpace < index))
				lastSpace = date.Length - 1;
			
			// Remove stuff between parentheses.
			if((date[index] == '(') && (date[lastSpace] == ')'))
			{
				date = date.Remove(index - 1, date.Length - index);
				
				index = date.LastIndexOfAny(new char[] { '-', '+'});
				lastSpace = date.Length - 1;
			}
			
			string utcOffset = string.Empty;
			if((date[index] == '-') || (date[index] == '+'))
			{
				index++;
				lastSpace++;
				utcOffset = date.Substring(index, lastSpace - index);
				index--;
				lastSpace--;
				
				date = date.Substring(0, index);
			}
			
			int offsetHours = 0;
			int.TryParse(utcOffset, out offsetHours);
			offsetHours /= 100;
			TimeSpan offset = new TimeSpan(Math.Abs(offsetHours), 0, 0);
			
			// Remove any etra character at the end of the string.
			for(int i = date.Length - 1; i > -1; i--)
			{
				if(char.IsDigit(date[i]))
				{
					i++;
					date = date.Remove(i, date.Length - i);
					break;
				}
			}

			// Checks for different date format.
			int day;
			int.TryParse(date.Substring(0, 1), out day);
			if(day > 0)
				dateFormat = dateFormat.Replace("ddd dd", "d");
			
			day = 0;
			int.TryParse(date.Substring(4, 2), out day);
			if((day > 0) && (day < 10))
				dateFormat = dateFormat.Replace("ddd dd", "ddd d");
			
			date = date.Trim();
			
			
			DateTime dt = new DateTime(0);
			try
			{
				dt = DateTime.ParseExact(
					date,
					dateFormat,
					CultureInfo.InvariantCulture);
				
				// Add global UTC offset and remove local UTC offset.
				dt += offset;
				dt += TimeZone.CurrentTimeZone.GetUtcOffset(dt);
			}
			catch(FormatException)
			{
				dt = new DateTime(0);
			}
			
			return dt;
		}
		
		private static DateTime GetDate(string s)
		{
			var date = s.Replace("date:", string.Empty).Trim();
			date = MailParsingUtils.RemoveParenthesisEnding(date);
			date = MailParsingUtils.RemoveCharEnding(date);
			
			return DateTime.Parse(date);
		}
		
		private Encoding GetEncoding(string s)
		{ // Encoding.UTF8 is the default encoding.
			
			string encoding;
			int index = s.IndexOf("charset=") + 8; // 8: size of charset=
			
			if(index == 0)
				return Encoding.UTF8;
			
			encoding = s.SubstringEx('"','"',index);
			
			// In case there is no quotation marks.
			if(encoding == string.Empty)
				encoding = s.Substring(index, s.Length - index);
			
			if(encoding.Contains(";"))
			{
				index = encoding.IndexOf(';');
				encoding = encoding.Substring(0, index);
			}

			if(s.Contains("charset="))
				return Encoding.GetEncoding(encoding);
			else
				return Encoding.UTF8;
		}
		
		private string GetBody(Encoding charset = null)
		{
			// "Compile-time constant".
			if(charset == null)
				charset = Encoding.UTF8;
			
			var lBody = new List<string>();
			var body = string.Empty;
			int bodyStart = Int32.MaxValue;
			
			bool contentEncoded = false;
			if(lines
			   .IndexOf("Content-Transfer-Encoding: base64") != -1)
				contentEncoded = true;
			
			if(contentEncoded)
			{
				bodyStart = lines.IndexOf(string.Empty) + 1;
				lBody = lines.GetRange(bodyStart, lines.Count - bodyStart);
				string encodedBody = string.Join("", lBody.ToArray());
				string decodedBody = charset.GetString(
					Convert.FromBase64String(encodedBody));
				string[] decodedArray = decodedBody.Split(
					new string[] { "\r\n" }, 
					StringSplitOptions.RemoveEmptyEntries);
				
				// Replaces the encoded ones with the decoded ones
				lines.RemoveRange(bodyStart, lines.Count - bodyStart);
				lines.AddRange(decodedArray);
				lBody = new List<string>();
				
			}
			
			int htmlBegin = -1;
			int htmlEnd = -1;
			for(int i = 0; i < lines.Count; i++)
			{
				string current = lines[i];
				
				if((i > bodyStart) && current.StartsWith("Content-Type:")
				   && (charset == Encoding.UTF8))
				{
					charset = GetEncoding(current);
				}
				
				if(i > bodyStart)
				{
					if((htmlBegin == -1)
					   && (current.StartsWith("<html>") ||
					       current.StartsWithEx("<!DOCTYPE html")))
						htmlBegin = i;
					
					if((htmlEnd == -1) && current.StartsWith("</html>"))
						htmlEnd = i;
					
					// Sometimes lines end with = sign.
					if(current.EndsWith("="))
						current = current.Remove(current.Length - 1 , 1);
					current = MailDecoder.DecodeSpecialChars(current, charset);
					
					// Just in case there is no HTML.
					lBody.Add(current);
					
					lines[i] = current;
					// If bodyStart is already set, don't need to check again.
					continue;
				}
				
				// Gets the first empty line (end of the headers).
				if(string.IsNullOrWhiteSpace(current))
				{
					// Skips blank lines
					
					int offset = i + 1;
					while(string.IsNullOrWhiteSpace(lines[offset]))
						offset++;
					
					bodyStart = offset;
				}
			}
			
			if((htmlBegin != -1) && (htmlBegin < htmlEnd))
			{
				lBody = lines.GetRange(htmlBegin, htmlEnd - htmlBegin);
			}

			
			// If there is no HTML.
			if(lBody.Count == 0)
			{
				int emptyLine = 0;
				foreach(var l in lines)
				{
					int currentOffset = lines.IndexOf(l);
					if(emptyLine == 0)
					{
						if(string.IsNullOrWhiteSpace(l))
						{
							emptyLine = currentOffset + 1;
							break;
						}
					}
					
					if(currentOffset > emptyLine)
					{
						string current = lines[currentOffset];
						// Sometimes lines end with = sign.
						if(current.EndsWith("="))
							current = current.Remove(current.Length - 1 , 1);
						current = MailDecoder.DecodeSpecialChars(current,
						                                         Message.CharSet);
					}
				}
				
				lBody = lines.GetRange(emptyLine, lines.Count - emptyLine);
			}
			

			body = string.Join(string.Empty, lBody.ToArray());
			
			return body;
		}
		
		private bool CheckForHTML()
		{
			var kv = CheckForHtml();
			
			if((kv.Key != -1) && (kv.Value != -1))
				return true;
			else
				return false;
		}
		
		private KeyValuePair<int, int> CheckForHtml()
		{
			int begin = Int32.MaxValue;
			int end =  Int32.MaxValue;
			
			for(int i = 0; i < lines.Count; i++)
			{
				if((end == Int32.MaxValue) && lines[i].StartsWith("<html>") ||
				   lines[i].StartsWithEx("<!DOCTYPE html"))
				{
					begin = i;
					continue;
				}
				
				if((begin < i) && lines[i].StartsWith("</html"))
				{
					end = i;
					break;
				}
			}
			
			// Check for errors.
			if((begin == Int32.MaxValue) || (end == Int32.MaxValue)
			   || (begin > end))
			{
				begin = -1;
				end = -1;
			}
			
			return new KeyValuePair<int, int>(begin, end);
		}

	}
}