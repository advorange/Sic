using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

namespace Sic.Core
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

		protected FileImageDetails(
			DateTimeOffset createdAt,
			string source,
			IImageDetails details)
			: this(createdAt, source, details.Original, details.Thumbnail) { }

		public static async Task<IFileImageDetails> CreateAsync(string path, int size)
		{
			var created = File.GetCreationTimeUtc(path);
			var bytes = await File.ReadAllBytesAsync(path).CAF();
			var details = ImageDetails.Create(bytes, size);
			return new FileImageDetails(created, path, details);
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
	}
}