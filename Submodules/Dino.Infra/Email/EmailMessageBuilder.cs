using System.Collections.Generic;

namespace Dino.Infra.Email
{
	public class EmailMessageBuilder : IMessageBuilder
	{
		private const char SurroundingChar = '%';

		public string BuildMessage(string source, IDictionary<string, string> valuesToReplace)
		{
			var message = source;

			foreach (var currKey in valuesToReplace.Keys)
			{
				message = message.Replace(SurroundingChar + currKey + SurroundingChar, valuesToReplace[currKey]);
			}

			return message;
		}
	}
}
