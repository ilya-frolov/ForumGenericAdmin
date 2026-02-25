using System;
using System.Security.Cryptography;
using Dino.Common.Helpers;

namespace Dino.Common.Security
{
	public enum PasswordStrength
	{
		Unacceptable = 1,
		Weak = 2,
		Ok = 3,
		Strong = 4,
		Secure = 5
	}

	/// <summary>
	/// This class contains tools for working with passwords.
	/// </summary>
	public class PasswordTools
	{
		#region CTor

		/// <summary>
		/// CTor
		/// </summary>
		private PasswordTools()
		{
		}

		#endregion

		#region Hash

		/// <summary>
		/// Generates a salted hash, using SHA-256.
		/// </summary>
		/// <param name="plainTextPassword">The password (as plain-text).</param>
		/// <param name="salt">The salt for the hash.</param>
		/// <returns>The hashed password.</returns>
		public static byte[] GenerateSaltedHashSHA256(string plainTextPassword, byte[] salt)
		{
			// Get the password and the salt as byte arrays.
			byte[] plainText = plainTextPassword.ToByteArray();

			HashAlgorithm algorithm = new SHA256Managed();

			byte[] plainTextWithSaltBytes =
			  new byte[plainText.Length + salt.Length];

			for (int i = 0; i < plainText.Length; i++)
			{
				plainTextWithSaltBytes[i] = plainText[i];
			}
			for (int i = 0; i < salt.Length; i++)
			{
				plainTextWithSaltBytes[plainText.Length + i] = salt[i];
			}

			return algorithm.ComputeHash(plainTextWithSaltBytes);
		}


		/// <summary>
		/// Generates a salted hash, using SHA-256, with a salt.
		/// </summary>
		/// <param name="plainTextPassword">The password (as plain-text).</param>
		/// <param name="salt">The salt for the hash.</param>
		/// <param name="saltLength">The length of the salt.</param>
        /// <returns>The hashed password.</returns>
		public static byte[] GenerateSaltedHashSHA256(string plainTextPassword, out byte[] salt, int saltLength = 32)
        {
			// Get salt.
            salt = GenerateRandomSalt(saltLength);

            return GenerateSaltedHashSHA256(plainTextPassword, salt);
        }



		#endregion

		#region Salt

		/// <summary>
		/// Generates a random salt (for hashing a password).
		/// </summary>
		/// <param name="saltLength">The length of the salt.</param>
		/// <returns>The random salt.</returns>
		public static byte[] GenerateRandomSalt(int saltLength = 32)
		{
			// Create a crypto-service provider.
			var cryptoProvider = new RNGCryptoServiceProvider();

			// Fix the salt length if needed.
			if (saltLength <= 0)
			{
				saltLength = 0;
			}

			// TODO: If 0, will generate from the settings.

			// Declare the salt's array.
			var salt = new byte[saltLength];

			// Generate a salt.
			cryptoProvider.GetBytes(salt);

			// Return the salt.
			return (salt);
		}

		#endregion

		#region Comparison

		/// <summary>
		/// Comparing a given password with a stored password, using SHA256.
		/// </summary>
		/// <param name="comparisonPassword">The password for comparison.</param>
		/// <param name="storedPassword">The stored password.</param>
		/// <param name="storedSalt">The salt of the stored password.</param>
		/// <returns></returns>
		public static bool ComparePasswordsSHA256(string comparisonPassword, byte[] storedPassword,
			byte[] storedSalt)
		{
			// Get the new password hashed, as byte-array.
			byte[] comparisonHashedPassword = GenerateSaltedHashSHA256(comparisonPassword, storedSalt);

			// Compare.
			return (CompareByteArrays(comparisonHashedPassword, storedPassword));
		}

		/// <summary>
		/// Compares two byte array (for passwords).
		/// </summary>
		/// <param name="array1">The first byte array for comparison.</param>
		/// <param name="array2">The seconds byte array for comparison.</param>
		/// <returns>Are the arrays equal.</returns>
		private static bool CompareByteArrays(byte[] array1, byte[] array2)
		{
			if (array1.Length != array2.Length)
			{
				return false;
			}

			for (int i = 0; i < array1.Length; i++)
			{
				if (array1[i] != array2[i])
				{
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Policy

		/// <summary>
		/// Returns a password's strength.
		/// </summary>
		/// <param name="password">The password.</param>
		/// <returns>The password's strength.</returns>
		public static PasswordStrength GetPasswordStrength(string password)
		{
			// Calculate score.
			int score = CalculatePasswordScore(password);

			// Check the strength according to the score.
			PasswordStrength strength;
			if (score < 50)
				strength = PasswordStrength.Unacceptable;
			else if (score < 60)
				strength = PasswordStrength.Weak;
			else if (score < 80)
				strength = PasswordStrength.Ok;
			else if (score < 100)
				strength = PasswordStrength.Strong;
			else
				strength = PasswordStrength.Secure;

			// Result.
			return (strength);
		}

		/// <summary>
		/// Calculates a password's score.
		/// </summary>
		/// <param name="password">The password.</param>
		/// <returns>The password's score.</returns>
		private static int CalculatePasswordScore(string password)
		{
			int score = 0;

			// Check that we've got a valid password.
			if (!password.IsNullOrEmpty())
			{
				// Length score.
				score += Math.Min(10, password.Length) * 6;

				// Count the number of differenet character types.
				int digits = 0;
				int uppers = 0;
				int lowers = 0;
				int symbols = 0;
				foreach (var ch in password)
				{
					if (char.IsUpper(ch))
						uppers++;
					else if (char.IsLower(ch))
						lowers++;
					else if (char.IsDigit(ch))
						digits++;
					else // if (char.IsSymbol(ch))
						symbols++;
				}

				// Calculate score.
				score += Math.Min(2, lowers) * 5;			// Lower characters score.
				score += Math.Min(2, uppers) * 5;			// Upper characters score.
				score += Math.Min(2, digits) * 5;			// Digit characters score.
				score += Math.Min(2, symbols) * 5;			// Symbol characters score.
			}

			return (score);
		}

		#endregion
	}
}
