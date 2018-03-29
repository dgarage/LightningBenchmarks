using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.CLightning
{
	public class NodeInfo
	{
		public static bool TryParse(string str, out NodeInfo nodeInfo)
		{
			nodeInfo = null;
			var parts = str.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
			if(parts.Length < 2)
				return false;
			var nodeId = parts[0];
			if(!PubKey.Check(Encoders.Hex.DecodeData(nodeId), true))
				return false;
			var host = String.Join("@", parts.Skip(1).ToArray());
			parts = host.Split(new[] { ':' }, StringSplitOptions.None);
			var maybePort = parts[parts.Length - 1];
			if(ushort.TryParse(maybePort, out ushort port))
			{
				host = string.Join(":", parts.Take(parts.Length - 1).ToArray());
			}
			else
			{
				port = 9735;
			}
			if(string.IsNullOrWhiteSpace(host))
				return false;
			nodeInfo = new NodeInfo(nodeId, host, port);
			return true;
		}
		public NodeInfo(string nodeId, string host, int port)
		{
			if(host == null)
				throw new ArgumentNullException(nameof(host));
			if(nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
			Port = port;
			Host = host;
			NodeId = nodeId;
		}
		public string NodeId
		{
			get; private set;
		}
		public string Host
		{
			get; private set;
		}
		public int Port
		{
			get; private set;
		}

		public override string ToString()
		{
			return $"{NodeId}@{Host}:{Port}";
		}
	}
}
