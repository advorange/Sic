using System.Collections.Generic;
using System.Threading.Tasks;

using AdvorangesSettingParser.Implementation;

using AdvorangesUtils;

using Sic.Core;

namespace Sic.Console
{
	public sealed class Program
	{
		private static async Task Main(string[] args)
		{
			ConsoleUtils.PrintingFlags &= ~ConsolePrintingFlags.RemoveMarkdown;

			var fileHandler = new FileHandler();
			var imageComparer = new ImageComparer();

#if TRUE
			args = new[]
			{
				"-s",
				@"D:\Test",
			};
#endif

			var parseArgs = new ParseArgs(args, new[] { '"' }, new[] { '"' });
			fileHandler.SettingParser.Parse(parseArgs);

			var files = fileHandler.GetImageFiles();
			var i = 0;
			await foreach (var file in imageComparer.CacheFiles(files))
			{
				ConsoleUtils.WriteLine($"[{++i}] Successfully processed {file.Source}.");
				ConsoleUtils.DebugWrite($"[{i}] Hash length: {file.Original.Hash.Length}");
			}
			var duplicates = new List<string>();
			var j = 0;
			await foreach (var file in imageComparer.GetDuplicates())
			{
				ConsoleUtils.WriteLine($"[{++j}] Successfully found a duplicate: {file.Source}.");
				duplicates.Add(file.Source);
			}

			fileHandler.MoveFiles(duplicates);
			ConsoleUtils.WriteLine($"Successfully moved {duplicates.Count} duplicate files to {fileHandler.Destination}.");
		}
	}
}