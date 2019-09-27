using System.Diagnostics;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

namespace Sic.Core.Hashes
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	[DebuggerTypeProxy(typeof(HashDetails))]
	public sealed class BrightnessHash : HashDetails
	{
		public BrightnessHash(int size, string hash) : base(size, hash)
		{
		}

		public BrightnessHash(int width, int height, string hash) : base(width, height, hash)
		{
		}

		public override bool IsSimilar(IHashDetails other, double similarity)
		{
			if (Hash.Length != other.Hash.Length || !(other is BrightnessHash))
			{
				return false;
			}

			//If the aspect ratio is too different then don't bother checking the hash
			var margin = 1 - similarity;
			var xAspect = this.GetAspectRatio();
			var yAspect = other.GetAspectRatio();
			if (xAspect > yAspect * (1 + margin) || xAspect < yAspect * (1 - margin))
			{
				return false;
			}

			var matchCount = 0;
			var xHash = Hash;
			var yHash = other.Hash;
			for (var i = 0; i < xHash.Length; ++i)
			{
				if (xHash[i] == yHash[i])
				{
					++matchCount;
				}
			}
			return (matchCount / (float)xHash.Length) >= similarity;
		}
	}
}