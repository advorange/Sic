using System;
using System.Diagnostics;

using Sic.Core.Abstractions;

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

		public FileImageDetails(DateTimeOffset createdAt, string source, IImageDetails details)
			: this(createdAt, source, details.Original, details.Thumbnail) { }

		public FileImageDetails(
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

		public override bool Equals(object obj)
		{
			if (obj is null)
			{
				return false;
			}
			else if (ReferenceEquals(this, obj))
			{
				return true;
			}
			else if (obj is IFileImageDetails other)
			{
				return Equals(other);
			}
			return false;
		}

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