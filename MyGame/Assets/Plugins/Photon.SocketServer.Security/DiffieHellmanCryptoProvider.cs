using Photon.SocketServer.Numeric;
using System;
using System.Security.Cryptography;

namespace Photon.SocketServer.Security
{
	internal class DiffieHellmanCryptoProvider : IDisposable
	{
		private static readonly BigInteger primeRoot = new BigInteger(OakleyGroups.Generator);

		private readonly BigInteger prime;

		private readonly BigInteger secret;

		private readonly BigInteger publicKey;

		private Rijndael crypto;

		private byte[] sharedKey;

		public bool IsInitialized
		{
			get
			{
				return this.crypto != null;
			}
		}

		/// <summary>
		/// Gets the public key that can be used by another DiffieHellmanCryptoProvider object 
		/// to generate a shared secret agreement.
		/// </summary>
		public byte[] PublicKey
		{
			get
			{
				return this.publicKey.GetBytes();
			}
		}

		/// <summary>
		///  Gets the shared key that is used by the current instance for cryptographic operations.
		/// </summary>
		public byte[] SharedKey
		{
			get
			{
				return this.sharedKey;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Photon.SocketServer.Security.DiffieHellmanCryptoProvider" /> class.
		/// </summary>
		public DiffieHellmanCryptoProvider()
		{
			this.prime = new BigInteger(OakleyGroups.OakleyPrime768);
			this.secret = this.GenerateRandomSecret(160);
			this.publicKey = this.CalculatePublicKey();
		}

		/// <summary>
		/// Derives the shared key is generated from the secret agreement between two parties, 
		/// given a byte array that contains the second party's public key. 
		/// </summary>
		/// <param name="otherPartyPublicKey">
		/// The second party's public key.
		/// </param>
		public void DeriveSharedKey(byte[] otherPartyPublicKey)
		{
			BigInteger key = new BigInteger(otherPartyPublicKey);
			BigInteger sKey = this.CalculateSharedKey(key);
			this.sharedKey = sKey.GetBytes();
			byte[] hash = default(byte[]);
			using (SHA256 hashProvider = new SHA256Managed())
			{
				hash = hashProvider.ComputeHash(this.SharedKey);
			}
			this.crypto = new RijndaelManaged();
			this.crypto.Key = hash;
			this.crypto.IV = new byte[16];
			this.crypto.Padding = PaddingMode.PKCS7;
		}

		public byte[] Encrypt(byte[] data)
		{
			return this.Encrypt(data, 0, data.Length);
		}

		public byte[] Encrypt(byte[] data, int offset, int count)
		{
			using (ICryptoTransform enc = this.crypto.CreateEncryptor())
			{
				return enc.TransformFinalBlock(data, offset, count);
			}
		}

		public byte[] Decrypt(byte[] data)
		{
			return this.Decrypt(data, 0, data.Length);
		}

		public byte[] Decrypt(byte[] data, int offset, int count)
		{
			using (ICryptoTransform enc = this.crypto.CreateDecryptor())
			{
				return enc.TransformFinalBlock(data, offset, count);
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				return;
			}
		}

		private BigInteger CalculatePublicKey()
		{
			return DiffieHellmanCryptoProvider.primeRoot.ModPow(this.secret, this.prime);
		}

		private BigInteger CalculateSharedKey(BigInteger otherPartyPublicKey)
		{
			return otherPartyPublicKey.ModPow(this.secret, this.prime);
		}

		private BigInteger GenerateRandomSecret(int secretLength)
		{
			BigInteger result;
			do
			{
				result = BigInteger.GenerateRandom(secretLength);
			}
			while (result >= this.prime - (BigInteger)1 || result == (BigInteger)0);
			return result;
		}
	}
}
