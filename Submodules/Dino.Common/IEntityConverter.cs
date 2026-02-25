namespace Dino.Common
{
	public interface IEntityConverter<in TSource, out TDest>
	{
		TDest Convert(TSource source);
	}
}
