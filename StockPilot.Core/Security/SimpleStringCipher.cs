using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StockPilot.Security
{
	public static class SimpleStringCipherCustom
	{
		private static readonly byte[] InitVectorBytes = Encoding.ASCII.GetBytes("jkE49230Tf093b42");

		private const int Keysize = 256;

		public const string DefaultPassPhrase = "gsKnGZ041HLL4IM8";

		public static string Encrypt(string plainText, string passPhrase = "gsKnGZ041HLL4IM8")
		{
			byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
			{
				byte[] keyBytes = password.GetBytes(32);
				using (RijndaelManaged symmetricKey = new RijndaelManaged())
				{
					symmetricKey.Mode = CipherMode.CBC;
					using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, InitVectorBytes))
					{
						using (MemoryStream memoryStream = new MemoryStream())
						{
							using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
							{
								cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
								cryptoStream.FlushFinalBlock();
								return Convert.ToBase64String(memoryStream.ToArray());
							}
						}
					}
				}
			}
		}

		public static string Decrypt(string cipherText, string passPhrase = "gsKnGZ041HLL4IM8")
		{
			byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
			using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
			{
				byte[] keyBytes = password.GetBytes(32);
				using (RijndaelManaged symmetricKey = new RijndaelManaged())
				{
					symmetricKey.Mode = CipherMode.CBC;
					using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, InitVectorBytes))
					{
						using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
						{
							using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
							{
								byte[] plainTextBytes = new byte[cipherTextBytes.Length];
								int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
								return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
							}
						}
					}
				}
			}
		}
	}
}
