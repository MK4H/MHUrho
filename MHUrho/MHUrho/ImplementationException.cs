using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MHUrho
{
	/// <summary>
	/// Exception thrown when there is an error in the implementation of the program
	/// </summary>
	class ImplementationException : ApplicationException
	{
		public ImplementationException()
		{
		}

		public ImplementationException(string message) : base(message)
		{
		}

		public ImplementationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ImplementationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
