using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dino.Common.Security
{
	public static class AesCryptHelper
	{
		/// <summary>
		/// Encrypts a string using AES
		/// </summary>
		/// <param name="key">The key to encrypt with</param>
		/// <param name="salt">The salt to encrypt with</param>
		/// <param name="clearData">The text to encrypt</param>
		/// <returns>The encrypted data</returns>
		public static byte[] Encrypt(string key, string salt, string clearData)
		{
			byte[] encryptedBytes;
			var saltBytes = Encoding.Unicode.GetBytes(salt);
			var clearBytes = Encoding.Unicode.GetBytes(clearData);
			using (var encryptor = Aes.Create())
			{
				var pdb = new Rfc2898DeriveBytes(key, saltBytes);
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (var ms = new MemoryStream())
				{
					using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
					{
						cs.Write(clearBytes, 0, clearBytes.Length);
						cs.Close();
					}

					encryptedBytes = ms.ToArray();
				}
			}
			return encryptedBytes;
		}

		/// <summary>
		/// Decryps an encrypted string
		/// </summary>
		/// <param name="key">The key to decrypt with</param>
		/// <param name="salt">The salt to decrypt with</param>
		/// <param name="encryptedData">The data to decrypt</param>
		/// <returns>The decrypted string</returns>
		public static string Decrypt(string key, string salt, byte[] encryptedData)
		{
			string clearData;

			var saltBytes = Encoding.Unicode.GetBytes(salt);

			using (var encryptor = Aes.Create())
			{
				var pdb = new Rfc2898DeriveBytes(key, saltBytes);
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (var ms = new MemoryStream())
				{
					using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(encryptedData, 0, encryptedData.Length);
						cs.Close();
					}

					clearData = Encoding.Unicode.GetString(ms.ToArray());
				}
			}
			return clearData;
		}
	}
}
