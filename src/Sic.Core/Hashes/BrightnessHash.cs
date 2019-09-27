using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Sic.Core.Abstractions;
using Sic.Core.Utils;

using SixLabors.ImageSharp.PixelFormats;

namespace Sic.Core.Hashes
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public sealed class BrightnessHash : HashDetails
	{
		private BrightnessHash(int width, int height, string hash) : base(width, height, hash)
		{
		}

		public static IHashDetails Create(ReadOnlySpan<Rgba32> pixels, int width, int height)
		{
			var total = 0f;
			var brightnesses = new float[pixels.Length];
			for (var i = 0; i < pixels.Length; ++i)
			{
				var pixel = pixels[i];
				var brightness = GetBrightness(pixel.A, pixel.R, pixel.G, pixel.B);
				brightnesses[i] = brightness;
				total += brightness;
			}

			var avg = total / pixels.Length;
			var chars = new char[pixels.Length];
			for (var i = 0; i < pixels.Length; ++i)
			{
				chars[i] = brightnesses[i] > avg ? '1' : '0';
			}

			return new BrightnessHash(width, height, new string(chars));
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetBrightness(byte a, byte r, byte g, byte b)
		{
			//Magic numbers for caclulating brightness, see:
			//https://stackoverflow.com/a/596243
			const float R_MULT = 0.299f;
			const float G_MULT = 0.587f;
			const float B_MULT = 0.114f;
			const float A_VALS = 255f;
			return ((R_MULT * r) + (G_MULT * g) + (B_MULT * b)) * (a / A_VALS);
		}
	}
}