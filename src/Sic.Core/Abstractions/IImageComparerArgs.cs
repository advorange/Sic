namespace Sic.Core.Abstractions
{
	public interface IImageComparerArgs
	{
		int ImagesPerTask { get; }
		int ThumbnailSize { get; }
	}
}