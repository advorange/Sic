using System.Collections.Generic;

namespace Sic.Core.Abstractions
{
	public interface IFileHandler
	{
		IEnumerable<string> GetImageFiles();

		void MoveFiles(IEnumerable<string> files);
	}
}