using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
	class ResourceLoadingException : ApplicationException
	{

		public ResourceLoadingException(string resourceName)
			:base(FormMessage(resourceName))
		{

		}

		public ResourceLoadingException(string resourceName, Exception innerException)
			: base(FormMessage(resourceName), innerException)
		{
		}

		static string FormMessage(string resourceName)
		{
			return $"Loading resource \"{resourceName}\" failed, resource not found.";
		}
	}
}
