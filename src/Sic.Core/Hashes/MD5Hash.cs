using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Sic.Core.Abstractions;

namespace Sic.Core.Hashes
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public sealed class MD5Hash : HashDetails
	{
		private MD5Hash(int width, int height, string hash) : base(width, height, hash)
		{
		}

		public static IHashDetails Create(ReadOnlySpan<byte> bytes, int width, int height)
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
			return new MD5Hash(width, height, hash);
		}

		public override bool IsSimilar(IHashDetails other, double similarity)
					=> false;
	}
}