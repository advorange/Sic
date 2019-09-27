using System;
using System.IO;
using AdvorangesSettingParser.Implementation;
using AdvorangesSettingParser.Implementation.Instance;
using AdvorangesSettingParser.Interfaces;
using AdvorangesSettingParser.Utils;
using Sic.Core;
using Sic.Core.Abstractions;

namespace Sic.Console
{
	internal sealed class Args : IFileHandlerArgs, IImageComparerArgs, IParsable
	{
		private DirectoryInfo? _Destination;

		public DirectoryInfo Destination
		{
			get => _Destination ?? Source.CreateSubdirectory("Duplicates");
			set => _Destination = value;
		}

		public int ImagesPerTask { get; set; } = ImageComparerArgs.Default.ImagesPerTask;
		public bool IsRecursive { get; set; }
		public SettingParser SettingParser { get; set; }
		public DirectoryInfo Source { get; set; } = null!;
		public int ThumbnailSize { get; set; } = ImageComparerArgs.Default.ThumbnailSize;

		private Args()
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
					Description = "The directory to look through.",
					CannotBeNull = true,
				},
				new Setting<DirectoryInfo>(() => Destination, new[] { "d" }, TryParseUtils.TryParseDirectoryInfo)
				{
					Description = "The directory to move duplicates to.",
					IsOptional = true,
					CannotBeNull = false,
				},
				new Setting<int>(() => ImagesPerTask, new[] { "i" })
				{
					Description = "The amount of images to cache per task. The lower the number, the faster but more resource intensive.",
					IsOptional = true,
				},
				new Setting<int>(() => ThumbnailSize, new[] { "t" })
				{
					Description = "The default size to make the thumbnail when comparing.",
					IsOptional = true,
				}
			};
		}

		public static Args Parse(string[] args)
		{
			var parseArgs = new ParseArgs(args, new[] { '"' }, new[] { '"' });
			var actualArgs = new Args();
			var result = actualArgs.SettingParser.Parse(parseArgs);
			if (result.Errors.Count > 0 || result.UnusedParts.Count > 0)
			{
				throw new ArgumentException(nameof(args));
			}
			return actualArgs;
		}
	}
}