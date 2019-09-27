using System;
using System.Diagnostics;

using Sic.Core.Abstractions;

namespace Sic.Core
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ImageDetails : IImageDetails, IEquatable<IImageDetails>
	{
		public IHashDetails Original { get; }
		public IHashDetails Thumbnail { get; }

		private string DebuggerDisplay
			=> $"Width: {Original.Width}, Height: {Original.Height}";

		public ImageDetails(IHashDetails original, IHashDetails thumbnail)
		{
			Original = original;
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
			else if (obj is IImageDetails other)
			{
				return Equals(other);
			}
			return false;
		}

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
	}
}