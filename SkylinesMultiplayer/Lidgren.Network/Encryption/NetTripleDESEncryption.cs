using System;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network
{
	//TODO: Readd this later when the main repo is fixed and contains this class.
	/*
	public class NetTripleDESEncryption : NetCryptoProviderBase
	{
		public NetTripleDESEncryption(NetPeer peer)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
		}

		public NetTripleDESEncryption(NetPeer peer, string key)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
			SetKey(key);
		}

		public NetTripleDESEncryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}*/
}
