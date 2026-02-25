namespace Dino.Common.Helpers
{
	public static class BooleanHelpers
	{
		public static int ToInt(this bool val)
		{
			return val ? 1 : 0;
		}
	}
}
