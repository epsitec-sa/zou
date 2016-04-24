using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epsitec.GetText;
using NGettext;

namespace Lib2
{
    public class Class_fr_CH
    {
		public Class_fr_CH()
		{
			//t._ ("Hello World from Lib2");
			///// Check for fuzzy stuff...
			//t._ ("Hello Worelede from Lib2");
			//t._ ("Hello World from Lib2");

			this.catalog.GetString ("Hello World from Lib2");
			/// Check for fuzzy stuff...
			this.catalog.GetString ("Hello Worelede from Lib2");
			this.catalog.GetString ("Hello World from Lib2");
		}

		//private readonly T t = new T("Lib2", "fr-CH");
		private readonly ICatalog catalog = AppDomain.CurrentDomain.CreateCatalog("Lib2", "fr-CH");
	}
}
