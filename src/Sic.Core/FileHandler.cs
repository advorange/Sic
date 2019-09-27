using System.Collections.Generic;
using System.IO;

using AdvorangesUtils;

using Sic.Core.Abstractions;

namespace Sic.Core
{
	public sealed class FileHandler : IFileHandler
	{
		private readonly IFileHandlerArgs _Args;

		public FileHandler(IFileHandlerArgs args)
		{
			_Args = args;
		}

		public IEnumerable<string> GetImageFiles()
		{
			foreach (var file in GetFiles(_Args.Source))
			{
				if (file.FullName.IsImagePath())
				{
					yield return file.FullName;
				}
			}
		}

		public void MoveFiles(IEnumerable<string> files)
		{
			_Args.Destination.Create();

			var sourceDir = _Args.Source.FullName;
			var destDir = _Args.Destination.FullName;
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
			if (_Args.IsRecursive)
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