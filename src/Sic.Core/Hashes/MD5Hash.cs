using System.Diagnostics;

using Sic.Core.Abstractions;

namespace Sic.Core.Hashes
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public sealed class MD5Hash : HashDetails
	{
		public MD5Hash(int size, string hash) : base(size, hash)
		{
		}

		public MD5Hash(int width, int height, string hash) : base(width, height, hash)
		{
		}

		public override bool IsSimilar(IHashDetails other, double similarity)
			=> false;
	}
}