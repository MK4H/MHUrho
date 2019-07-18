using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// An exception representing unrecoverable runtime error.
	/// </summary>
	class FatalRuntimeException : Exception
	{
		public FatalRuntimeException()
		{
		}

		public FatalRuntimeException(string message) : base(message)
		{
		}

		public FatalRuntimeException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
