using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sic.Core
{
	public class ImageComparer : IImageComparer
	{
		private readonly IImageComparerArgs _Args;

		private readonly ConcurrentDictionary<string, IFileImageDetails> _ImageDetails
			= new ConcurrentDictionary<string, IFileImageDetails>();

		public IReadOnlyCollection<IFileImageDetails> ImageDetails => _ImageDetails.Values.ToArray();

		public event Action<IFileImageDetails>? FileCached;

		public ImageComparer() : this(ImageComparerArgs.Default)
		{
		}

		public ImageComparer(IImageComparerArgs args)
		{
			_Args = args;
		}

		public async IAsyncEnumerable<IFileImageDetails> CacheFilesAsync(IEnumerable<string> paths)
		{
			foreach (var path in paths)
			{
				if (!File.Exists(path))
				{
					continue;
				}

				var details = await CreateFileDetailsAsync(path, _Args.ThumbnailSize).CAF();
				_ImageDetails.AddOrUpdate(path, (_) => details, (_, __) => details);
				FileCached?.Invoke(details);
				yield return details;
			}
		}

		public async Task CacheFilesGrouped(IEnumerable<string> paths)
		{
			var tasks = paths.GroupInto(_Args.ImagesPerTask).Select(x => Task.Run(async () =>
			{
				foreach (var path in x)
				{
					if (!File.Exists(path))
					{
						continue;
					}

					var details = await CreateFileDetailsAsync(path, _Args.ThumbnailSize).CAF();
					_ImageDetails.AddOrUpdate(path, (_) => details, (_, __) => details);
					FileCached?.Invoke(details);
				}
			}));
			await Task.WhenAll(tasks).CAF();
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

			var largerLater = await CreateFileDetailsAsync(x.Source, size).CAF();
			var largetEarlier = await CreateFileDetailsAsync(y.Source, size).CAF();
			return AreSimilar(largerLater, largetEarlier, similarity);
		}

		protected HashDetails CreateBrightnessHash(Image<Rgba32> image, int size)
		{
			image.Mutate(x => x.Resize(size, 0));
			var pixels = image.GetPixelSpan();

			var total = 0f;
			var brightnesses = new float[pixels.Length];
			for (var i = 0; i < pixels.Length; ++i)
			{
				var pixel = pixels[i];
				var brightness = GetBrightness(pixel.A, pixel.R, pixel.G, pixel.B);
				brightnesses[i] = brightness;
				total += brightness;
			}

			var avg = total / pixels.Length;
			var chars = new char[pixels.Length];
			for (var i = 0; i < pixels.Length; ++i)
			{
				chars[i] = brightnesses[i] > avg ? '1' : '0';
			}

			return new HashDetails(image.Width, image.Height, new string(chars));
		}

		protected async Task<IFileImageDetails> CreateFileDetailsAsync(string path, int size)
		{
			var created = File.GetCreationTimeUtc(path);
			var bytes = await File.ReadAllBytesAsync(path).CAF();
			var details = CreateImageDetails(bytes, size);
			return new FileImageDetails(created, path, details);
		}

		protected IImageDetails CreateImageDetails(ReadOnlySpan<byte> bytes, int size)
		{
			using var img = Image.Load<Rgba32>(bytes);

			var original = CreateMD5Hash(img, bytes);
			var thumbnail = CreateBrightnessHash(img, size);
			return new ImageDetails(original, thumbnail);
		}

		protected HashDetails CreateMD5Hash(Image<Rgba32> image, ReadOnlySpan<byte> bytes)
		{
			Span<byte> destination = new byte[16];

			using var md5 = MD5.Create();
			{
				if (!md5.TryComputeHash(bytes, destination, out var written))
				{
					throw new InvalidOperationException("Unable to compute MD5 hash.");
				}
			}

			var array = destination.ToArray();
			var hash = BitConverter.ToString(array).Replace("-", "").ToLower();
			return new HashDetails(image.Width, image.Height, hash);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected float GetBrightness(byte a, byte r, byte g, byte b)
		{
			//Magic numbers for caclulating brightness, see:
			//https://stackoverflow.com/a/596243
			const float R_MULT = 0.299f;
			const float G_MULT = 0.587f;
			const float B_MULT = 0.114f;
			const float A_VALS = 255f;
			return ((R_MULT * r) + (G_MULT * g) + (B_MULT * b)) * (a / A_VALS);
		}
	}
}