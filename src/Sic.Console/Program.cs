using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AdvorangesSettingParser.Implementation;

using AdvorangesUtils;

using Sic.Core;
using Sic.Core.Abstractions;

namespace Sic.Console
{
	public sealed class Program
	{
		private static async Task Main(string[] args)
		{
			ConsoleUtils.PrintingFlags &= ~ConsolePrintingFlags.RemoveMarkdown;

			var fileHandler = new FileHandler();
			var imageComparer = new ImageComparer();

#if DEBUG && TRUE
			args = new[]
			{
				"-s",
				@"D:\Test - Copy",
			};
#endif

			var parseArgs = new ParseArgs(args, new[] { '"' }, new[] { '"' });
			fileHandler.SettingParser.Parse(parseArgs);

			var files = fileHandler.GetImageFiles();
			var i = 0;
			await foreach (var file in imageComparer.CacheFiles(files))
			{
				ConsoleUtils.WriteLine($"[#{++i}] Processed {file.Source}.");
			}
			System.Console.WriteLine();

			var duplicates = new List<string>();
			var j = 0;
			await foreach (var file in imageComparer.GetDuplicates(progress: new DuplicateProgress()))
			{
				ConsoleUtils.WriteLine($"[#{++j}] Found a duplicate: {file.Source}.", ConsoleColor.DarkYellow);
				duplicates.Add(file.Source);
			}
			System.Console.WriteLine();

			fileHandler.MoveFiles(duplicates);
			ConsoleUtils.WriteLine($"Moved {duplicates.Count} duplicates to {fileHandler.Destination}.");
		}

		private sealed class DuplicateProgress : IProgress<IFileImageDetails>
		{
			private int _Count;

			public void Report(IFileImageDetails value)
				=> ConsoleUtils.WriteLine($"[#{++_Count}] Found no duplicates for: {value.Source}.");
		}
	}
}