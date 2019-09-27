using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

namespace Sic.Core
{
	public class ImageComparer : IImageComparer
	{
		private readonly IImageComparerArgs _Args;

		private readonly ConcurrentDictionary<string, IFileImageDetails> _ImageDetails
			= new ConcurrentDictionary<string, IFileImageDetails>();

		public IReadOnlyCollection<IFileImageDetails> ImageDetails => _ImageDetails.Values.ToArray();

		public ImageComparer() : this(ImageComparerArgs.Default)
		{
		}

		public ImageComparer(IImageComparerArgs args)
		{
			_Args = args;
		}

		public async IAsyncEnumerable<IFileImageDetails> CacheFilesAsync(IEnumerable<string> paths)
		{
			await foreach (var details in new BackgroundCachingAsyncEnumerable(paths, _Args))
			{
				_ImageDetails.AddOrUpdate(details.Source, _ => details, (_, __) => details);
				yield return details;
			}
		}

		public async IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(
			double similarity = 1,
			IProgress<IFileImageDetails>? progress = null)
		{
			var details = _ImageDetails.Values.ToList();
			var count = details.Count;
			for (int i = count - 1, c = 1; i > 0; --i, ++c)
			{
				var later = details[i];
				for (var j = i - 1; j >= 0; --j)
				{
					//If not even close to similar then don't test more
					var earlier = details[j];
					if (!AreSameData(earlier, later)
						&& !await AreSimilarAsync(earlier, later, similarity).CAF())
					{
						continue;
					}

					//If earlier (lower index) is newer than later (higher index)
					//then the item to remove is 'earlier'
					//the index to remove at is 'j' (earlier's index)
					//the amount to decrease the indexer by is '2'
					//It's 2 because we need to account for an early index being removed
					var laterNewer = later.CreatedAt > earlier.CreatedAt;
					var removeInfo = new
					{
						Item = laterNewer ? later : earlier,
						Index = laterNewer ? i : j,
						Delta = laterNewer ? 2 : 1,
					};

					//Remove stuff from the cache and the iterating list
					_ImageDetails.TryRemove(removeInfo.Item.Source, out _);
					details.RemoveAt(removeInfo.Index);
					i -= removeInfo.Delta;

					yield return removeInfo.Item;
				}
				progress?.Report(later);
			}
		}

		public IAsyncEnumerable<IFileImageDetails> GetDuplicatesAsync(double similarity = 1)
			=> GetDuplicatesAsync(similarity, null);

		protected bool AreSameData(IImageDetails x, IImageDetails y)
			=> x.Original.Hash == y.Original.Hash;

		protected bool AreSimilar(IImageDetails x, IImageDetails y, double similarity)
		{
			if (x.Thumbnail.Hash.Length != y.Thumbnail.Hash.Length)
			{
				return false;
			}

			//If the aspect ratio is too different then don't bother checking the hash
			var margin = 1 - similarity;
			var xAspect = x.Thumbnail.Width / (float)x.Thumbnail.Height;
			var yAspect = y.Thumbnail.Width / (float)y.Thumbnail.Height;
			if (xAspect > yAspect * (1 + margin) || xAspect < yAspect * (1 - margin))
			{
				return false;
			}

			var matchCount = 0;
			var xHash = x.Thumbnail.Hash;
			var yHash = y.Thumbnail.Hash;
			for (var i = 0; i < xHash.Length; ++i)
			{
				if (xHash[i] == yHash[i])
				{
					++matchCount;
				}
			}
			return (matchCount / (float)xHash.Length) >= similarity;
		}

		protected async Task<bool> AreSimilarAsync(
			IFileImageDetails x,
			IFileImageDetails y,
			double similarity)
		{
			if (!AreSimilar(x, y, similarity))
			{
				return false;
			}

			//Check once again but with a higher resolution
			var size = 512;
			size = Math.Min(size, x.Original.Width);
			size = Math.Min(size, x.Original.Height);
			size = Math.Min(size, y.Original.Width);
			size = Math.Min(size, y.Original.Height);

			var largerLater = await HashingUtils.CreateFileDetailsAsync(x.Source, size).CAF();
			var largetEarlier = await HashingUtils.CreateFileDetailsAsync(y.Source, size).CAF();
			return AreSimilar(largerLater, largetEarlier, similarity);
		}
	}
}