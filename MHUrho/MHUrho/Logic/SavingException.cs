using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
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
