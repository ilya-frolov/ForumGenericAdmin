using System;

namespace Dino.Common.Culture
{
	[Flags]
	public enum WeekDays
	{
		None = 0,
		Sunday = 1,
		Monday = 2,
		Tuesday = 4,
		Wednesday = 8,
		Thursday = 16,
		Friday = 32,
		Saturday = 64,
		All = 128
	}
}
