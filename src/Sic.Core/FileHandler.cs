using System.Collections.Generic;
using System.IO;

using AdvorangesSettingParser.Implementation.Instance;
using AdvorangesSettingParser.Interfaces;
using AdvorangesSettingParser.Utils;

using AdvorangesUtils;

namespace Sic.Core
{
	public sealed class FileHandler : IParsable
	{
		private DirectoryInfo? _Destination;

		public DirectoryInfo Destination
		{
			get => _Destination ?? Source.CreateSubdirectory("Duplicates");
			set => _Destination = value;
		}

		public bool IsRecursive { get; set; }
		public SettingParser SettingParser { get; set; }
		public DirectoryInfo Source { get; set; } = null!;

		public FileHandler()
		{
			SettingParser = new SettingParser
			{
				new Setting<bool>(() => IsRecursive, new[] { "r" })
				{
					Description = "Whether to search for files deeper than the current directory.",
					IsFlag = true,
				},
				new Setting<DirectoryInfo>(() => Source, new[] { "s" }, TryParseUtils.TryParseDirectoryInfo)
				{
					Description = "The directory to look through."
				},
				new Setting<DirectoryInfo>(() => Destination, new[] { "d" }, TryParseUtils.TryParseDirectoryInfo)
				{
					Description = "The directory to move duplicates to.",
					IsOptional = true,
					CannotBeNull = false,
				},
			};
		}

		public IEnumerable<string> GetImageFiles()
		{
			foreach (var file in GetFiles(Source))
			{
				if (file.FullName.IsImagePath())
				{
					yield return file.FullName;
				}
			}
		}

		public void MoveFiles(IEnumerable<string> files)
		{
			Destination.Create();

			var sourceDir = Source.FullName;
			var destDir = Destination.FullName;
			foreach (var source in files)
			{
				var destination = source.Replace(sourceDir, destDir);
				File.Delete(destination);
				File.Move(source, destination);
			}
		}

		/*
		private async Task CopyFileAsync(string sourcePath, string destinationPath)
		{
			const FileOptions OPTIONS = FileOptions.Asynchronous | FileOptions.SequentialScan;
			const int BUFFER = 4096;

			using var old = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER, OPTIONS);
			using var copy = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BUFFER, OPTIONS);
			await old.CopyToAsync(copy).CAF();
		}*/

		private IEnumerable<FileInfo> GetFiles(DirectoryInfo path)
		{
			if (IsRecursive)
			{
				foreach (var dir in path.EnumerateDirectories())
				{
					foreach (var file in GetFiles(dir))
					{
						yield return file;
					}
				}
			}

			foreach (var file in path.EnumerateFiles())
			{
				yield return file;
			}
		}
	}
}