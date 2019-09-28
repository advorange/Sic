using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

namespace Sic.Core.Details
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class FileImageDetails : IFileImageDetails, IEquatable<IFileImageDetails>
	{
		public DateTimeOffset CreatedAt { get; }
		public IHashDetails Original { get; }
		public string Source { get; }
		public IHashDetails Thumbnail { get; }

		private string DebuggerDisplay
			=> $"Source: {Source}, Created At: {CreatedAt}, Width: {Original.Width}, Height: {Original.Height}";

		protected FileImageDetails(
			DateTimeOffset createdAt,
			string source,
			IHashDetails original,
			IHashDetails thumbnail)
		{
			CreatedAt = createdAt;
			Original = original;
			Source = source;
			Thumbnail = thumbnail;
		}

		public static async Task<IFileImageDetails> CreateAsync(string path, int size)
		{
			var created = File.GetCreationTimeUtc(path);
			var bytes = await File.ReadAllBytesAsync(path).CAF();
			var details = ImageDetails.Create(bytes, size);
			return new FileImageDetails(created, path, details.Original, details.Thumbnail);
		}

		public static async Task<IImageDetails> CreateAsync(
			HttpResponseMessage response,
			int size)
		{
			var created = DateTimeOffset.UtcNow;
			var source = response.RequestMessage.RequestUri.ToString();
			var bytes = await response.Content.ReadAsByteArrayAsync().CAF();
			var details = ImageDetails.Create(bytes, size);
			return new FileImageDetails(created, source, details.Original, details.Thumbnail);
		}

		public override bool Equals(object obj)
			=> this.Equals<IFileImageDetails>(obj);

		public bool Equals(IFileImageDetails other)
		{
			if (other is null)
			{
				return false;
			}
			return CreatedAt == other.CreatedAt
				&& Original == other.Original
				&& Source == other.Source
				&& Thumbnail == other.Thumbnail;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = (int)2166136261;
				hash = (hash * 16777619) ^ CreatedAt.GetHashCode();
				hash = (hash * 16777619) ^ Original.GetHashCode();
				hash = (hash * 16777619) ^ Source.GetHashCode();
				hash = (hash * 16777619) ^ Thumbnail.GetHashCode();
				return hash;
			}
		}

		public bool IsSameData(IImageDetails other)
			=> Original.Hash == other.Original.Hash;

		public async Task<bool> IsSimilarAsync(IImageDetails other, double similarity = 1)
		{
			if (!Thumbnail.IsSimilar(other.Thumbnail, similarity) || !(other is IFileImageDetails fileOther))
			{
				return false;
			}

			//Check once again but with a higher resolution
			var size = this.GetSmallestSize(fileOther);
			var x = await CreateAsync(Source, size).CAF();
			var y = await CreateAsync(fileOther.Source, size).CAF();
			return x.Thumbnail.IsSimilar(y.Thumbnail, similarity);
		}
	}
}