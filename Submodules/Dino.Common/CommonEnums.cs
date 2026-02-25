namespace Dino.Common
{
	public enum LoginResult
	{
		SystemError = 0,
		IncorrectEmail = 1,
		IncorrectPassword = 2,
		AccountIsNotActivated = 3,
		Success = 4,
        AccountBlocked = 5,
        AccountLocked = 6,
        OtpRequired = 7,
        InvalidOtpCode = 8,
	}

	public enum RegistrationResult
	{
		SystemError = 0,
		EmailAlreadyRegistered = 1,
        UserNameTaken = 2,
		PasswordTooWeak = 3,
		Success = 4,
        PhoneAlreadyRegistered = 5,
		UserIsNotActive = 6
    }

	public enum FileType : short
	{
		Unknown = 0,
		Image = 1,
		Video = 2,
		Document = 3,
		Audio = 4
	}

	public enum SortDirection
	{
		/// <summary>Sort from smallest to largest —for example, from 1 to 10.</summary>
		Ascending,
		/// <summary>Sort from largest to smallest — for example, from 10 to 1.</summary>
		Descending,
	}
}
