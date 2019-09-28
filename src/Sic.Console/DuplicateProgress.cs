using System;
using System.Runtime.CompilerServices;
using AdvorangesUtils;

using Sic.Core.Abstractions;

namespace Sic.Console
{
	internal sealed class DuplicateProgress : IProgress<IFileImageDetails>
	{
		private readonly StrongBox<int> _Sb;

		public DuplicateProgress(StrongBox<int> sb)
		{
			_Sb = sb;
		}

		public void Report(IFileImageDetails value)
			=> ConsoleUtils.WriteLine($"[#{++_Sb.Value}] Found no duplicates: {value.Source}.");
	}
}