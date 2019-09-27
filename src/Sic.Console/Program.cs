using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core;

namespace Sic.Console
{
	internal sealed class Program
	{
		static Program()
		{
			ConsoleUtils.PrintingFlags &= ~ConsolePrintingFlags.RemoveMarkdown;
		}

		private readonly Args _Args;
		private readonly FileHandler _FileHandler;
		private readonly ImageComparer _ImageComparer;

		public Program(string[] args)
		{
#if DEBUG && TRUE
			args = new[]
			{
				"-s",
				@"D:\Test - Copy",
			};
#endif
			_Args = Args.Parse(args);
			_FileHandler = new FileHandler(_Args);
			_ImageComparer = new ImageComparer(_Args);
		}

		public async Task RunAsync()
		{
			await ProcessFilesAsync().CAF();
			await ProcessDuplicatesAsync().CAF();
		}

		private async Task ProcessFilesAsync()
		{
			var files = _FileHandler.GetImageFiles();
			var lockObj = new object();
			var i = 0;
			await foreach (var details in _ImageComparer.CacheFilesAsync(files))
			{
				lock (lockObj)
				{
					ConsoleUtils.WriteLine($"[#{++i}] Processed {details.Source}.");
				}
			}
			System.Console.WriteLine();
		}

		private async Task ProcessDuplicatesAsync()
		{
			var duplicates = new List<string>();
			var j = 0;
			await foreach (var file in _ImageComparer.GetDuplicatesAsync(progress: new DuplicateProgress()))
			{
				ConsoleUtils.WriteLine($"[#{++j}] Found a duplicate: {file.Source}.", ConsoleColor.DarkYellow);
				duplicates.Add(file.Source);
			}
			System.Console.WriteLine();

			_FileHandler.MoveFiles(duplicates);
			ConsoleUtils.WriteLine($"Moved {duplicates.Count} duplicates to {_Args.Destination}.");
		}

		private static Task Main(string[] args)
			=> new Program(args).RunAsync();
	}
}