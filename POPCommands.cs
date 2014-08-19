namespace POP
{
	internal static class POPCommands
	{
		
		#region Authorization
		
		/// <summary>Sends user's email address for authentification.</summary>
		/// <example>
		/// C: USER emailAddress <br/>
		/// S: OK/ERR
		/// </example>
		public const string USER = "USER";
		
		/// <summary>Sends password for authentification.</summary>
		/// <example>
		/// C: PASS password <br/>
		/// S: Mailbox contains x msg | ERR
		/// </example>
		public const string PASS = "PASS";
		
		#endregion
		
		
		#region Transaction
		
		/// <summary>Retrives a message form the server.</summary>
		/// <example>
		/// C: RETR msgID <br/>
		/// S: *message* <br/>
		/// ..
		/// </example>
		public const string RETRIEVE = "RETR";
		
		/// <summary>Lists all message on the server.</summary>
		/// <example>		
		/// C: LIST <br/>
		/// S: msgID nbOfBytes <br/>
		/// ..
		/// </example>
		public const string LISTALL = "LIST";
		
		/// <summary>Lists a single message from the server.</summary>
		/// <example>
		/// C: LIST msgID <br/>
		/// S: msgID nbOfBytes
		/// </example>
		public const string LISTMSG = "LIST";
		
		/// <summary>Deletes a message from the server.</summary>
		/// <example>
		/// C: DELE msgID <br/>
		/// S: OK/ERR
		/// </example>
		public const string DELETE = "DELE";
		
		/// <summary>Does nothing.</summary>
		/// <remarks>Mostly used for testing.</remarks>
		/// <example>
		/// C: NOOP <br/>
		/// S: OK
		/// </example>
		public const string NoOperation = "NOOP";
		
		/// <summary>Retrieves stats about the mail account.</summary>
		/// <example>
		/// C: STAT <br/>
		/// S: NumberOfMessages SizeOfAllMessages
		/// </example>
		public const string STAT = "STAT";
		
		/// <summary>Cancels deletion of messages.</summary>
		/// <remarks>Valid only for current session.</remarks>
		/// <example>
		/// C: RSET <br/>
		/// S: OK
		/// </example>
		public const string RESET = "RSET";
		
		/// <summary>Gets the top of a message.</summary>>
		/// <example>
		/// C: TOP msg n <br/>
		/// S: msg header + n first lines <br/>
		/// ..
		/// </example>
		public const string TOP = "TOP";
		
		/// <summary>Retrives a unique identifier for the message.</summary>
		/// <remarks>If no message ID is provided,
		/// the response will be multiline.</remarks>
		/// <example>
		/// C: UIDL msgID <br/>
		/// S: uid
		/// </example>
		public const string UIDL = "UIDL";
		
		#endregion
		
		
		/// <summary>Ends the POP3 session.</summary>
		/// <example>
		/// C: QUIT <br/>
		/// S: Tells what changed | Sign off message
		/// </example>
		public const string QUIT = "QUIT";
		
	}
}