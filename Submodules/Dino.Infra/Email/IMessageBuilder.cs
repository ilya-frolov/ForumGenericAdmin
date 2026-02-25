using System.Collections.Generic;

namespace Dino.Infra.Email
{
	public interface IMessageBuilder
	{
		/// <summary>
		/// Builds a message by replacing values in the source
		/// </summary>
		/// <param name="source">The source message</param>
		/// <param name="valuesToReplace">The values to replace in the message</param>
		/// <returns>The message with replaced values</returns>
		string BuildMessage(string source, IDictionary<string, string> valuesToReplace);
	}
}
