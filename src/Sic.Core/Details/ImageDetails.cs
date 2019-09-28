using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Hashes;
using Sic.Core.Utils;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sic.Core.Details
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ImageDetails : IImageDetails, IEquatable<IImageDetails>
	{
		public IHashDetails Original { get; }
		public IHashDetails Thumbnail { get; }

		private string DebuggerDisplay
			=> $"Width: {Original.Width}, Height: {Original.Height}";

		protected ImageDetails(IHashDetails original, IHashDetails thumbnail)
		{
			Original = original;
			Thumbnail = thumbnail;
		}

		public static IImageDetails Create(ReadOnlySpan<byte> bytes, int size)
		{
			using var img = Image.Load<Rgba32>(bytes);

			var original = MD5Hash.Create(bytes, img.Width, img.Height);

			img.Mutate(x => x.Resize(size, 0));
			var pixels = img.GetPixelSpan();
			var thumbnail = BrightnessHash.Create(pixels, img.Width, img.Height);

			return new ImageDetails(original, thumbnail);
		}

		public static async Task<IImageDetails> CreateAsync(Stream stream, int size)
		{
			using var ms = new MemoryStream();

			await stream.CopyToAsync(ms).CAF();

			return Create(ms.ToArray(), size);
		}

		public override bool Equals(object obj)
			=> this.Equals<IImageDetails>(obj);

		public bool Equals(IImageDetails other)
		{
			if (other is null)
			{
				return false;
			}
			return Original == other.Original
				&& Thumbnail == other.Thumbnail;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = (int)2166136261;
				hash = (hash * 16777619) ^ Original.GetHashCode();
				hash = (hash * 16777619) ^ Thumbnail.GetHashCode();
				return hash;
			}
		}

		public bool IsSameData(IImageDetails other)
			=> Original.Hash == other.Original.Hash;

		public Task<bool> IsSimilarAsync(IImageDetails other, double similarity = 1)
			=> Task.FromResult(false);
	}
}