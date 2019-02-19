using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Threading
{
	class MethodInvocationException : Exception
	{
		public MethodInvocationException(string message, Exception innerException)
			:base(message, innerException)
		{

		}

	}
}
