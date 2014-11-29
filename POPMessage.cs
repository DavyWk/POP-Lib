using System;
using System.Text;
using System.Collections.Generic;

namespace POP
{
	public class POPMessage
	{
		public string ID { get; set; }
		public List<Person> Receivers { get; set; }
		public Person Sender { get; set; }
		public string Body { get; set; }
		public string Subject { get; set; }
		public DateTime ArrivalTime { get; set; }
		public bool ContainsHTML { get; set; }
		public List<string> Header { get; set; }
		public List<string> Raw { get; set; }
		
		public POPMessage()
		{
			
		}
	}

}
