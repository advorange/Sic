using System.Collections.Generic;

namespace Sic.Core.Abstractions
{
	public interface IImageComparer
	{
		IReadOnlyCollection<IFileImageDetails> ImageDetails { get; }

		IAsyncEnumerable<IFileImageDetails> CacheFiles(IEnumerable<string> paths);

		IAsyncEnumerable<IFileImageDetails> GetDuplicates(double similarity = 1);
	}
}