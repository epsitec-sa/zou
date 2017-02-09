using System;
using Epsitec.Localization;

namespace Lib2
{
	public class Class_CurrentUICulture : ILocalizable<Class_CurrentUICulture>
	{
		public Class_CurrentUICulture()
		{
			Console.WriteLine (">>>> Lib2.Class_CurrentUICulture");
			Console.WriteLine (this.GetString ("Hello World from Lib2"));
			Console.WriteLine (this.GetString ("Another string defined in Lib2"));
			Console.WriteLine (this.GetString ("Common string"));
			Console.WriteLine ("<<<< Lib2.Class_CurrentUICulture");
			Console.WriteLine ();

			new Lib1.Class_CurrentUICulture ();
		}
	}
}
