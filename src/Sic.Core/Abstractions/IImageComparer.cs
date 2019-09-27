using System.Collections.Generic;

namespace Sic.Core.Abstractions
{
	public interface IImageComparer
	{
		IReadOnlyCollection<IFileImageDetails> ImageDetails { get; }

		void Cache(IFileImageDetails details);

		IAsyncEnumerable<IFileImageDetails> CacheFilesAsync(IEnumerable<string> paths);

		IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(double similarity = 1);
	}
}