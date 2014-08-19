namespace POP
{
	public static class Constants
	{
		public const string MultiLineTerminator = ".";
			
		public const string Terminator = "\r\n";
		public const short TerminatorLength = 2;
		
		public const string OK = "+OK";
		public const string ERROR = "-ERR";
		
		// Custom
		/// <summary>
		/// If a message's ID is INVALID, then accessing the message's
		/// body field might throw an exception.
		/// </summary>
		public const string INVALID = "-1";
	}
}