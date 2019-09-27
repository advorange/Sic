using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core.Abstractions;
using Sic.Core.Hashes;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sic.Core.Utils
{
	public static class HashingUtils
	{
		public static IHashDetails CreateBrightnessHash(Image<Rgba32> image, int size)
		{
			image.Mutate(x => x.Resize(size, 0));
			var pixels = image.GetPixelSpan();

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

			return new BrightnessHash(image.Width, image.Height, new string(chars));
		}

		public static async Task<IFileImageDetails> CreateFileDetailsAsync(string path, int size)
		{
			var created = File.GetCreationTimeUtc(path);
			var bytes = await File.ReadAllBytesAsync(path).CAF();
			var details = CreateImageDetails(bytes, size);
			return new FileImageDetails(created, path, details);
		}

		public static IImageDetails CreateImageDetails(ReadOnlySpan<byte> bytes, int size)
		{
			using var img = Image.Load<Rgba32>(bytes);

			var original = CreateMD5Hash(img, bytes);
			var thumbnail = CreateBrightnessHash(img, size);
			return new ImageDetails(original, thumbnail);
		}

		public static IHashDetails CreateMD5Hash(Image<Rgba32> image, ReadOnlySpan<byte> bytes)
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
			return new MD5Hash(image.Width, image.Height, hash);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetBrightness(byte a, byte r, byte g, byte b)
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