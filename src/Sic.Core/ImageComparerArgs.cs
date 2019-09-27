using Sic.Core.Abstractions;

namespace Sic.Core
{
	public sealed class ImageComparerArgs : IImageComparerArgs
	{
		public static ImageComparerArgs Default { get; } = new ImageComparerArgs();

		public int ImagesPerTask => 500;
		public int ThumbnailSize => 25;

		private ImageComparerArgs()
		{
		}
	}
}