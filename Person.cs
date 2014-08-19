using System.Net;

namespace POP
{
	public class Person
	{
		public string Name { get; set; }
		public string EMailAddress { get; set; }
		
		public Person()
		{
			Name = string.Empty;
			EMailAddress = string.Empty;
		}
		public Person(string name, string emailAddress)
		{
			EMailAddress = emailAddress;
			Name = name;
		}
	}
	
}
