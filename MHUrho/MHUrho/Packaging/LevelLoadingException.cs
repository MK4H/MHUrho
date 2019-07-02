using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
	/// <summary>
	/// Exception representing a failure during loading.
	/// </summary>
	public class LevelLoadingException : ApplicationException
	{
		public LevelLoadingException()
		{

		}

		public LevelLoadingException(string message)
			: base(message)
		{

		}

		public LevelLoadingException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
