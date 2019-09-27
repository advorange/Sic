namespace Sic.Core.Abstractions
{
	public interface IHashDetails
	{
		string Hash { get; }
		int Height { get; }
		int Width { get; }

		bool IsSimilar(IHashDetails other, double similarity);
	}
}