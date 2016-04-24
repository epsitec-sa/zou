using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epsitec.GetText;
using NGettext;

namespace Lib1
{
	class Class_CurrentUICulture
	{
		public Class_CurrentUICulture()
		{
			//t._ ("Hello World from Lib1");
			this.catalog.GetString ("Hello World from Lib1");
			this.catalog.GetString ("Another string defined in Lib1");
		}

		//private readonly T t = new T("Lib1");
		private readonly ICatalog catalog = AppDomain.CurrentDomain.CreateCatalog("Lib1");
	}
}
