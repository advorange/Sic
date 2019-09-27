using System;
using System.Diagnostics;

using Sic.Core.Abstractions;

namespace Sic.Core
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public sealed class HashDetails : IHashDetails, IEquatable<IHashDetails>
	{
		public string Hash { get; }
		public int Height { get; }
		public int Width { get; }

		private string DebuggerDisplay
			=> $"Width: {Width}, Height: {Height}, Hash: {Hash}";

		public HashDetails(int size, string hash)
		{
			Hash = hash;
			Height = size;
			Width = size;
		}

		public HashDetails(int width, int height, string hash)
		{
			Hash = hash;
			Height = height;
			Width = width;
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
			else if (obj is IHashDetails other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(IHashDetails other)
		{
			if (other is null)
			{
				return false;
			}
			return Height == other.Height
				&& Width == other.Width
				&& Hash == other.Hash;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = (int)2166136261;
				hash = (hash * 16777619) ^ Hash.GetHashCode();
				hash = (hash * 16777619) ^ Height.GetHashCode();
				hash = (hash * 16777619) ^ Width.GetHashCode();
				return hash;
			}
		}
	}
}