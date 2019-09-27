using System.IO;

namespace Sic.Core.Abstractions
{
	public interface IFileHandlerArgs
	{
		DirectoryInfo Destination { get; }
		bool IsRecursive { get; }
		DirectoryInfo Source { get; }
	}
}