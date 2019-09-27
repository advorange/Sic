using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Details;

namespace Sic.Core
{
	internal sealed class BackgroundCachingAsyncEnumerator : IAsyncEnumerator<IFileImageDetails>
	{
		private readonly IImageComparerArgs _Args;
		private readonly CancellationToken _CancellationToken;
		private readonly IEnumerable<string> _Paths;
		private readonly ConcurrentQueue<IFileImageDetails> _Queue = new ConcurrentQueue<IFileImageDetails>();
		private int _FinishedProcessing;
		private int _IsStarted;
		public IFileImageDetails Current { get; private set; } = null!;

		public BackgroundCachingAsyncEnumerator(
			IEnumerable<string> paths,
			IImageComparerArgs args,
			CancellationToken cancellationToken)
		{
			_Paths = paths;
			_Args = args;
			_CancellationToken = cancellationToken;
		}

		public ValueTask DisposeAsync()
			=> new ValueTask();

		public async ValueTask<bool> MoveNextAsync()
		{
			if (_FinishedProcessing == 1 && _Queue.IsEmpty)
			{
				return false;
			}

			//Start the tasks if not already running
			if (Interlocked.Exchange(ref _IsStarted, 1) == 0)
			{
				_ = StartProcessingAsync();
			}

			//Wait until something is in the cache
			while (_Queue.IsEmpty)
			{
				await Task.Delay(10, _CancellationToken).CAF();
			}

			if (!_Queue.TryDequeue(out var details))
			{
				throw new InvalidOperationException("Unable to retrieve from queue.");
			}

			Current = details;
			return true;
		}

		private async Task StartProcessingAsync()
		{
			var groups = _Paths.GroupInto(_Args.ImagesPerTask);
			var tasks = groups.Select(x => Task.Run(async () =>
			{
				foreach (var path in x)
				{
					_CancellationToken.ThrowIfCancellationRequested();
					if (!File.Exists(path))
					{
						continue;
					}

					var size = _Args.ThumbnailSize;
					var details = await FileImageDetails.CreateAsync(path, size).CAF();
					_Queue.Enqueue(details);
				}
			}, _CancellationToken));
			try
			{
				await Task.WhenAll(tasks).CAF();
			}
			catch (TaskCanceledException)
			{
				//Just treat TaskCanceledException as an early end of processing
			}
			Interlocked.Exchange(ref _FinishedProcessing, 1);
		}
	}
}