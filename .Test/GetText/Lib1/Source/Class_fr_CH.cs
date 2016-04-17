using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epsitec.GetText;

namespace Lib1
{
    public class Class_fr_CH
    {
		public Class_fr_CH()
		{
			t._ ("Hello World from Lib1");
			/// Check for fuzzy stuff...
			t._ ("Hello Worelede from Lib1");
			t._ ("Hello World from Lib1");
		}

		private readonly T t = new T("Lib1", "fr-CH");
    }
}
