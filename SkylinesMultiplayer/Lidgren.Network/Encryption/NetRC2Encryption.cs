using System;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network
{
	//TODO: Readd this later when the main repo is fixed and contains this class.
	/*
	public class NetRC2Encryption : NetCryptoProviderBase
	{
		public NetRC2Encryption(NetPeer peer)
			: base(peer, new RC2CryptoServiceProvider())
		{
		}

		public NetRC2Encryption(NetPeer peer, string key)
			: base(peer, new RC2CryptoServiceProvider())
		{
			SetKey(key);
		}

		public NetRC2Encryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new RC2CryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}*/
}
