using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using AdvorangesUtils;

using Sic.Core;
using Sic.Core.Abstractions;

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
			var files = await ProcessFilesAsync().CAF();
			await ProcessDuplicatesAsync(files).CAF();
		}

		private async Task<IEnumerable<IFileImageDetails>> ProcessFilesAsync()
		{
			var files = _FileHandler.GetImageFiles();
			var details = new List<IFileImageDetails>();
			var i = 0;
			await foreach (var file in _ImageComparer.GetFilesAsync(files))
			{
				ConsoleUtils.WriteLine($"[#{++i}] Processed {file.Source}.");
				details.Add(file);
			}
			System.Console.WriteLine();
			return details;
		}

		private async Task ProcessDuplicatesAsync(IEnumerable<IFileImageDetails> files)
		{
			var duplicates = new List<string>();
			var sb = new StrongBox<int>();
			await foreach (var file in _ImageComparer.GetDuplicatesAsync(files, progress: new DuplicateProgress(sb)))
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