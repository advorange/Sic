using System;
using System.Diagnostics;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

namespace Sic.Core.Hashes
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public abstract class HashDetails : IHashDetails, IEquatable<IHashDetails>
	{
		public string Hash { get; }
		public int Height { get; }
		public int Width { get; }

		private string DebuggerDisplay
			=> $"Width: {Width}, Height: {Height}, Hash: {Hash}";

		protected HashDetails(int size, string hash)
		{
			Hash = hash;
			Height = size;
			Width = size;
		}

		protected HashDetails(int width, int height, string hash)
		{
			Hash = hash;
			Height = height;
			Width = width;
		}

		public override bool Equals(object obj)
			=> this.Equals<IHashDetails>(obj);

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

		public abstract bool IsSimilar(IHashDetails other, double similarity);
	}
}