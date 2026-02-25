namespace Dino.Infra.Email
{
	public class BasicEmailGenerator : IEmailGenerator
	{
		public string Subject { get; set; }
		public string Message { get; set; }

		public BasicEmailGenerator()
		{
			
		}

		public BasicEmailGenerator(string subject, string message)
		{
			Subject = subject;
			Message = message;
		}

		public string BuildSubject()
		{
			return Subject;
		}

		public string BuildMessage()
		{
			return Message;
		}
	}
}
