using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Details;

namespace Sic.Core
{
	public class ImageComparer : IImageComparer
	{
		private readonly IImageComparerArgs _Args;

		public ImageComparer() : this(ImageComparerArgs.Default)
		{
		}

		public ImageComparer(IImageComparerArgs args)
		{
			_Args = args;
		}

		public async IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(
			IEnumerable<IFileImageDetails> details,
			double similarity = 1,
			IProgress<IFileImageDetails>? progress = null)
		{
			var list = details.ToList();
			var count = list.Count;
			for (int i = count - 1, c = 1; i > 0; --i, ++c)
			{
				var late = list[i];
				for (var j = i - 1; j >= 0; --j)
				{
					//If not even close to similar then don't test more
					var early = list[j];
					if (!early.IsSameData(late) && !await early.IsSimilarAsync(late, similarity).CAF())
					{
						continue;
					}

					//If earlier (lower index) is newer than later (higher index)
					//then the item to remove is 'earlier'
					//the index to remove at is 'j' (earlier's index)
					//the amount to decrease the indexer by is '2'
					//It's 2 because we need to account for an early index being removed
					var laterNewer = late.CreatedAt > early.CreatedAt;
					var removeInfo = new
					{
						Item = laterNewer ? late : early,
						Index = laterNewer ? i : j,
						Delta = laterNewer ? 2 : 1,
					};

					//Remove stuff from the cache and the iterating list
					list.RemoveAt(removeInfo.Index);
					i -= removeInfo.Delta;

					yield return removeInfo.Item;
				}
				progress?.Report(late);
			}
		}

		public IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(
			IEnumerable<IFileImageDetails> details,
			double similarity = 1)
			=> GetDuplicatesAsync(details, similarity, null);

		public IAsyncEnumerable<IFileImageDetails> GetFilesAsync(IEnumerable<string> paths)
			=> new BackgroundCachingAsyncEnumerable(paths, this);

		protected virtual Task<IFileImageDetails> CreateAsync(string path, int size)
			=> FileImageDetails.CreateAsync(path, size);

		private sealed class BackgroundCachingAsyncEnumerable : IAsyncEnumerable<IFileImageDetails>
		{
			private readonly ImageComparer _Comparer;
			private readonly IEnumerable<string> _Paths;

			public BackgroundCachingAsyncEnumerable(
				IEnumerable<string> paths,
				ImageComparer comparer)
			{
				_Paths = paths;
				_Comparer = comparer;
			}

			public IAsyncEnumerator<IFileImageDetails> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				=> new BackgroundCachingAsyncEnumerator(_Paths, _Comparer, cancellationToken);
		}

		private sealed class BackgroundCachingAsyncEnumerator : IAsyncEnumerator<IFileImageDetails>
		{
			private readonly CancellationToken _CancellationToken;
			private readonly ImageComparer _Comparer;
			private readonly IEnumerable<string> _Paths;
			private readonly ConcurrentQueue<IFileImageDetails> _Queue = new ConcurrentQueue<IFileImageDetails>();
			private Exception? _ExceptionWhileProcessing;
			private int _FinishedProcessing;
			private int _IsStarted;
			public IFileImageDetails Current { get; private set; } = null!;

			public BackgroundCachingAsyncEnumerator(
				IEnumerable<string> paths,
				ImageComparer comparer,
				CancellationToken cancellationToken)
			{
				_Paths = paths;
				_Comparer = comparer;
				_CancellationToken = cancellationToken;
			}

			public ValueTask DisposeAsync()
				=> new ValueTask();

			public async ValueTask<bool> MoveNextAsync()
			{
				ThrowIfException();
				if ((_FinishedProcessing == 1 && _Queue.IsEmpty)
					|| _CancellationToken.IsCancellationRequested)
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
					ThrowIfException();
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
				try
				{
					var groups = _Paths.GroupInto(_Comparer._Args.ImagesPerTask);
					var tasks = groups.Select(x => Task.Run(async () =>
					{
						foreach (var path in x)
						{
							_CancellationToken.ThrowIfCancellationRequested();
							if (!File.Exists(path))
							{
								continue;
							}

							var size = _Comparer._Args.ThumbnailSize;
							var details = await _Comparer.CreateAsync(path, size).CAF();
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
				catch (Exception e)
				{
					_ExceptionWhileProcessing = e;
				}
			}

			private void ThrowIfException()
			{
				if (_ExceptionWhileProcessing != null)
				{
					ExceptionDispatchInfo.Capture(_ExceptionWhileProcessing).Throw();
				}
			}
		}
	}
}