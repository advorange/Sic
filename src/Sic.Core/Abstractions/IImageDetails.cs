namespace Sic.Core.Abstractions
{
	public interface IImageDetails
	{
		IHashDetails Original { get; }
		IHashDetails Thumbnail { get; }
	}
}