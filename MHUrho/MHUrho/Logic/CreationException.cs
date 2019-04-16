using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	class CreationException : ApplicationException
	{
		public CreationException()
		{

		}

		public CreationException(string message)
			: base(message)
		{

		}

		public CreationException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
