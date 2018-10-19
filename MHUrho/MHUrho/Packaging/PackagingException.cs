using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
    public class PackagingException : ApplicationException
    {
		public PackagingException()
		{

		}

		public PackagingException(string message)
			:base(message)
		{

		}

		public PackagingException(string message, Exception innerException)
			:base(message, innerException)
		{

		}
	}

	public class PackageLoadingException : PackagingException {


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

	public class FatalPackagingException : PackagingException {
		public FatalPackagingException()
		{

		}

		public FatalPackagingException(string message)
			: base(message)
		{

		}

		public FatalPackagingException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
