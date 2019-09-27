using System;

namespace Sic.Core.Abstractions
{
	public interface IFileImageDetails : IImageDetails
	{
		DateTimeOffset CreatedAt { get; }
		string Source { get; }
	}
}