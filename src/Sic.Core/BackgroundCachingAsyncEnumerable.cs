using System.Collections.Generic;
using System.Threading;

using Sic.Core.Abstractions;

namespace Sic.Core
{
	internal sealed class BackgroundCachingAsyncEnumerable : IAsyncEnumerable<IFileImageDetails>
	{
		private readonly IImageComparerArgs _Args;
		private readonly IEnumerable<string> _Paths;

		public BackgroundCachingAsyncEnumerable(IEnumerable<string> paths, IImageComparerArgs args)
		{
			_Paths = paths;
			_Args = args;
		}

		public IAsyncEnumerator<IFileImageDetails> GetAsyncEnumerator(
			CancellationToken cancellationToken = default)
			=> new BackgroundCachingAsyncEnumerator(_Paths, _Args, cancellationToken);
	}
}