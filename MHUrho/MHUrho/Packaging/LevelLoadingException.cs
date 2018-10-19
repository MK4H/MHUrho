using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
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
