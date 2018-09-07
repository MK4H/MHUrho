using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
    class PackageLoadingException : ApplicationException
    {
		public PackageLoadingException()
		{

		}

		public PackageLoadingException(string message)
			:base(message)
		{

		}

		public PackageLoadingException(string message, Exception innerException)
			:base(message, innerException)
		{

		}
	}
}
