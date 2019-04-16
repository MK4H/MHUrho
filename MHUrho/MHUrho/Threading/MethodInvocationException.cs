using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Threading
{
	class MethodInvocationException : ApplicationException
	{
		public MethodInvocationException(string message, Exception innerException)
			:base(message, innerException)
		{

		}

	}
}
