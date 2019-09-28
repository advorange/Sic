using System.Threading.Tasks;

namespace Sic.Core.Abstractions
{
	public interface IImageDetails
	{
		IHashDetails Original { get; }
		IHashDetails Thumbnail { get; }

		bool IsSameData(IImageDetails other);

		Task<bool> IsSimilarAsync(IImageDetails other, double similarity = 1);
	}
}