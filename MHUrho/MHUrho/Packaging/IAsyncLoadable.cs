using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MHUrho.Packaging
{
    interface IAsyncLoadable<T> {
		Task<T> StartLoading(LoadingWatcher loadingProgress);
	}
}
