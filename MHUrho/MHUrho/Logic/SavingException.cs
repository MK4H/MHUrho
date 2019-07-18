using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Exception used when there is a problem during saving and serialization of the level.
	/// </summary>
	public class SavingException : ApplicationException
	{
		public SavingException()
		{

		}

		public SavingException(string message)
			: base(message)
		{

		}

		public SavingException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
