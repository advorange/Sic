using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
		private readonly SystemFileHandler _FileHandler;
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
			_FileHandler = new SystemFileHandler(_Args);
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
			var i = 0;
			await foreach (var details in _ImageComparer.CacheFilesAsync(files))
			{
				ConsoleUtils.WriteLine($"[#{++i}] Processed {details.Source}.");
			}
			System.Console.WriteLine();
		}

		private async Task ProcessDuplicatesAsync()
		{
			var duplicates = new List<string>();
			var sb = new StrongBox<int>();
			await foreach (var file in _ImageComparer.GetDuplicatesAsync(progress: new DuplicateProgress(sb)))
			{
				const ConsoleColor DUPLICATE_FOUND = ConsoleColor.DarkYellow;
				const string REPORT = nameof(IProgress<int>.Report);

				ConsoleUtils.WriteLine($"[#{++sb.Value}] Found a duplicate: {file.Source}.", DUPLICATE_FOUND, REPORT);
				duplicates.Add(file.Source);
			}
			System.Console.WriteLine();

			_FileHandler.MoveFiles(duplicates);
			ConsoleUtils.WriteLine($"Moved {duplicates.Count} duplicate(s) to {_Args.Destination}.");
		}

		private static Task Main(string[] args)
			=> new Program(args).RunAsync();
	}
}