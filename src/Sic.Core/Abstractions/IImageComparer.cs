using System.Collections.Generic;

namespace Sic.Core.Abstractions
{
	public interface IImageComparer
	{
		IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(
			IEnumerable<IFileImageDetails> details,
			double similarity = 1);

		IAsyncEnumerable<IFileImageDetails> GetFilesAsync(IEnumerable<string> paths);
	}
}