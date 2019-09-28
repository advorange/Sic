using System;

using Sic.Core.Abstractions;

namespace Sic.Core.Utils
{
	public static class Utils
	{
		public static bool Equals<T>(this IEquatable<T> x, object y)
		{
			if (y is null)
			{
				return false;
			}
			else if (ReferenceEquals(x, y))
			{
				return true;
			}
			else if (y is T other)
			{
				return x.Equals(other);
			}
			return false;
		}

		public static float GetAspectRatio(this IHashDetails details)
			=> details.Width / (float)details.Height;

		public static int GetSmallestSize(this IImageDetails x, IImageDetails y, int largestAllowed = 512)
		{
			largestAllowed = Math.Min(largestAllowed, x.Original.Width);
			largestAllowed = Math.Min(largestAllowed, x.Original.Height);
			largestAllowed = Math.Min(largestAllowed, y.Original.Width);
			largestAllowed = Math.Min(largestAllowed, y.Original.Height);
			return largestAllowed;
		}
	}
}