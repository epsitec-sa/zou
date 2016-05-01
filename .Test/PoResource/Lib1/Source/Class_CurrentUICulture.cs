using System;
using Epsitec.Localization;
using NGettext;

namespace Lib1
{
	public class Class_CurrentUICulture: ILocalizable<Class_CurrentUICulture>
	{
		public Class_CurrentUICulture()
		{
			Console.WriteLine (">>>> Lib1.Class_CurrentUICulture");
			Console.WriteLine (this.GetString ("Hello World from Lib1"));
			Console.WriteLine (this.GetString ("Another string defined in Lib1"));
			Console.WriteLine ("<<<< Lib1.Class_CurrentUICulture");
			Console.WriteLine ();
		}
	}
}
