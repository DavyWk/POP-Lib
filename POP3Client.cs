using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;

using Utils;
using POP.CommandParsers;

namespace POP
{
	public class POP3Client : IDisposable
	{
		private bool disposed;
		private TcpClient client;
		private Stream stream;
		private const string invalidOperation =
			"Cannot execute this command in the {0} state";
		
		private readonly int _port;
		public int Port
		{
			get
			{
				return _port;
			}
		}
		
		private readonly string _host;
		public string Host
		{
			get
			{
				return _host;
			}
		}
		
		private readonly IPAddress _ip;
		public IPAddress IP
		{
			get
			{
				return _ip;
			}
		}
		
		private readonly bool _ssl;
		public bool SSL
		{
			get
			{
				return _ssl;
			}
		}
		
		public bool Connected
		{
			get
			{
				if((client != null) && (client.Client != null))
					return client.Client.Connected;
				else
					return false;
			}
		}
		
		public bool LoggedIn
		{
			get; private set;
		}
		
		public POPState State { get; private set; }
		
		
		public POP3Client(string host, int port, bool ssl = false)
		{
			_host = host;
			_port = port;
			_ssl = ssl;
			client = new TcpClient();
			State =  POPState.NONE;
			
			if(_port < 1)
			{
				// Port 995 is the default TLS/SSL POP3 port.
				_port = 995;
				_ssl = true;
			}

			IPAddress[] ips;
			try
			{
				ips = Dns.GetHostAddresses(Host);
			}
			catch
			{
				ips = new IPAddress[0];
				_ip = new IPAddress(0);
			}
			for(int i = 0; i < ips.Length; i++)
			{
				if(ips[i].AddressFamily == AddressFamily.InterNetwork)
				{
					_ip = ips[i];
					break;
				}
			}
		}
		
		#region Implementing IDisposable
		
		public void Close()
		{
			Dispose();
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if(disposed)
				return;
			
			if(disposing)
			{
				InternalClose();
			}
			
			disposed = true;
		}
		
		/// <summary>
		/// Cleans up ressources on forced exit.
		/// </summary>
		private void InternalClose()
		{
			if(stream != null)
			{
				stream.Close();
				stream = null;
			}
			
			if(client != null)
			{
				client.Close();
				client = null;
			}
			LoggedIn = false;
		}
		
		#endregion
		
		#region Internal Send/Receive functions
		// These functions will be public until I finish the public API.
		
		private void SendCommand(string format, params object[] args)
		{
			SendCommand(string.Format(format,args));
		}
		
		private void SendCommand(string command)
		{
			if(!client.Connected)
				Connect();
			
			try
			{
				var buffer = Encoding.UTF8.GetBytes(string.Concat(
					command,
					Constants.Terminator));
				
				stream.Write(buffer, 0, buffer.Length);
				stream.Flush();
			}
			catch (Exception ex)
			{
				if(ex is SocketException || ex is IOException)
				{
					
					InternalClose();
				}
				else
					throw;
			}

		}
		
		private List<string> ReceiveMultiLine()
		{
			var received = new List<string>();
			
			while(true)
			{
				string line = Receive();
				
				if(line == Constants.MultiLineTerminator)
					break;
				if(line.StartsWith(Constants.ERROR))
					break;
				
				received.Add(line);
			}
			
			return received;
		}
		
		private string Receive()
		{
			if(!Connected)
				Connect();
			
			var received = new List<byte>();
			string response;
			
			try
			{
				while(true)
				{
					int b = stream.ReadByte();
					if(b == Constants.Terminator[0] || b < 0)
						break;
					if(b != Constants.Terminator[1])
						received.Add((byte)b);
				}
			}
			catch (Exception ex)
			{
				if(ex is SocketException || ex is IOException)
				{
					InternalClose();
					//TODO: Fix crash here
				}
				else
					throw;
			}
			
			response = Encoding.UTF8.GetString(received.ToArray());
			
			return response;
		}
		
		#endregion
		
		/// <summary>
		/// Connects to the POP3 server.
		/// </summary>
		/// <returns>True if the connection was sucessful,
		/// false otherwise.</returns>
		public bool Connect(bool dummy = false)
		{
			return Protocol.CheckHeader(Connect());
		}
		
		/// <summary>
		/// Connects to the POP3 server.
		/// </summary>
		/// <returns>The welcome message or
		/// an empty string if the connection failed.</returns>
		public string Connect()
		{
			if(Connected)
			{
				Quit();
				InternalClose();
				
				client = new TcpClient();
			}
			
			if(client == null)
				client = new TcpClient();
			try
			{
				client.Connect(new IPEndPoint(IP, Port));
				client.ReceiveTimeout  = 10 * 1000;
				client.SendTimeout = 10 * 1000;
			}
			catch(SocketException ex)
			{
				InternalClose();
				
				return ex.Message;
			}
			
			stream = client.GetStream();

			State = POPState.Authorization;
			if(SSL)
			{
				State = POPState.NONE;
				var secureStream = new SslStream(stream);
				try
				{
					secureStream.AuthenticateAsClient(Host);
					
					stream = secureStream;
				}
				catch(IOException ex)
				{
					return ex.Message;
				}
				
				if(!secureStream.IsAuthenticated)
					return "Error while activating SSL";
				
				State = POPState.Authorization;
			}
			string response = Receive();
			if(string.IsNullOrEmpty(response))
				State = POPState.NONE;
			
			return response;
		}
		
		
		private string CheckConnection(bool dummy = false)
		{
			SendCommand(POPCommands.NoOperation);
			
			return Receive();
		}
		
		/// <summary>
		/// Checks if logged in or not
		/// </summary>
		/// <remarks>Mostly used for login limit check</remarks>
		private bool CheckConnection()
		{
			return Protocol.CheckHeader(CheckConnection(false));
		}

		#region POP Implementation
		
		public string Login(string emailAddress, string password)
		{
			if(State != POPState.Authorization)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			if(string.IsNullOrEmpty(emailAddress))
				emailAddress.ThrowIfNullOrEmpty("emailAddress");
			
			if(string.IsNullOrEmpty(password))
				password.ThrowIfNullOrEmpty("password");
			
			SendCommand("{0} {1}", POPCommands.USER, emailAddress);
			
			string response = Receive();
			if(!Protocol.CheckHeader(response))
			{ // invalid login
				LoggedIn = false;
				Quit();
				return response;
			}
			
			SendCommand("{0} {1}", POPCommands.PASS, password);
			
			response = Receive();

			if(Protocol.CheckHeader(response))
			{
				string check = CheckConnection(false);
				if(!Protocol.CheckHeader(check))
					return check;
				
				State = POPState.Transaction;
				LoggedIn = true;
			}

			return response;
		}
		
		/// <summary>Disconnects from the POP3 server.</summary>
		/// <returns>The server's exit meassage</returns>
		public string Quit()
		{
			// POP3 logic ... cf. RFC 1939 p10
			if(State == POPState.Transaction)
				State = POPState.Update;
			
			SendCommand(POPCommands.QUIT);
			LoggedIn = false;
			return Receive();
		}
		
		/// <summary>
		/// Gets the list messages stored on the server.
		/// </summary>
		/// <returns>A dictorinary of key-value pair where:<br/>
		/// 	Key: ID of the message <br/>
		/// 	Value: Size (in bytes)</returns>
		public Dictionary<int, int> ListMessages()
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand(POPCommands.LISTALL);
			Receive();
			List<string> received = ReceiveMultiLine();
			
			return ListParser.Parse(received);
		}
		
		public KeyValuePair<int, int> ListMessage(int msgID)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand("{0} {1}", POPCommands.LISTMSG, msgID);
			string received = Receive();
			if(!Protocol.CheckHeader(received))
				return new KeyValuePair<int, int>(msgID, -1);
			
			received = Protocol.RemoveHeader(received);
			var kv = ListParser.Parse(received);
			
			return kv;
		}
		
		/// <returns>The size of the message or
		/// -1 is the message does not exist.</returns>
		public int GetSize(int msgNumber)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand("{0} {1}", POPCommands.LISTMSG, msgNumber);
			string response = Receive();
			if(!Protocol.CheckHeader(response))
				return -1;
			response = Protocol.RemoveHeader(response);
			
			string[] elements = response.Split(' ');
			
			return int.Parse(elements[1]);
		}
		
		/// <summary>
		/// Gets all messages stored on the server.
		/// </summary>
		/// <remarks>Not optimized, can take minutes to execute.</remarks>
		public List<POPMessage> GetMessages()
		{
			var messageList = new List<POPMessage>();
			
			foreach(KeyValuePair<int,int> kv in ListMessages())
			{
				POPMessage m = GetMessage(kv.Key);
				if(m != null)
					messageList.Add(m);
			}
			
			return messageList;
		}
		
		/// <summary>
		/// Gets the message stored with "messageID" on the server. <br/>
		/// If the message does not exist, returns null.
		/// </summary>
		/// <returns>The message stored with "messageID" on the server,
		/// or null in case of error.</returns>
		public POPMessage GetMessage(int messageID)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand("{0} {1}", POPCommands.RETRIEVE, messageID);
			
			POPMessage ret = null;
			string response = Receive();
			if(Protocol.CheckHeader(response))
				ret = new MailParser(ReceiveMultiLine()).Message;
			else
			{
				ret = new POPMessage();
				ret.ID = Constants.INVALID;
				ret.Body = Protocol.RemoveHeader(response);
			}
			
			return ret;
		}
		
		/// <summary>
		/// Get statistics of the mailbox.
		/// </summary>
		/// <returns>A KeyValuePair where : <br/>
		/// Key: Number of messages <br/>
		/// Value: Size of all messages (in byte)</returns>
		public KeyValuePair<int, int> GetStats()
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand(POPCommands.STAT);
			
			return StatParser.Parse(Receive());
		}
		
		/// <summary>
		/// Deletes a message from the mailbox
		/// </summary>
		public string Delete(int messageID)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand("{0} {1}", POPCommands.DELETE, messageID);
			
			return Receive();
		}
		
		public bool Delete(int messageID, bool dummy = false)
		{
			return Protocol.CheckHeader(Delete(messageID));
		}
		
		/// <summary>
		/// Restores the messages deleted during the current session.
		/// </summary>
		public void Reset()
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand(POPCommands.RESET);
			// Just '+OK' so there is no need to log it
			Receive();
		}
		
		public List<string> Top(int messageID, int nLines)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand("{0} {1} {2}", POPCommands.TOP, messageID, nLines);
			
			var ret = ReceiveMultiLine();
			// Removes +OK/-ERR at the beginning.
			ret.RemoveAt(0);
			
			if(string.IsNullOrEmpty(ret[ret.Count - 1]))
				ret.RemoveAt(ret.Count -1);
			
			return ret;
		}
		
		public string GetUID(int messageID)
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));

			SendCommand("{0} {1}", POPCommands.UIDL, messageID);
			
			string received = Receive();
			
			if(!Protocol.CheckHeader(received))
			{
				received = Protocol.RemoveHeader(received);
				return received.Capitalize();
			}
			
			return UniqueIdentifierParser.Parse(received);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns>A dictionary where the key is the msgNumber and
		/// the value is the message's UID.</returns>
		public Dictionary<int, string> GetUID()
		{
			if(State != POPState.Transaction)
				throw new InvalidOperationException(
					string.Format(invalidOperation, State.ToString()));
			
			SendCommand(POPCommands.UIDL);
			
			var list = ReceiveMultiLine();
			var dic = new Dictionary<int, string>();
			
			if(!Protocol.CheckHeader(list[0]))
			{
				dic.Add(-1, list[0]);
				return dic;
			}
			
			list.RemoveAt(0);
			dic = UniqueIdentifierParser.Parse(list);
			
			return dic;
		}
		
		#endregion
		
	}
}