using System;

using AdvorangesUtils;

using Sic.Core.Abstractions;

namespace Sic.Console
{
	internal sealed class DuplicateProgress : IProgress<IFileImageDetails>
	{
		private int _Count;

		public void Report(IFileImageDetails value)
			=> ConsoleUtils.WriteLine($"[#{++_Count}] Found no duplicates for: {value.Source}.");
	}
}