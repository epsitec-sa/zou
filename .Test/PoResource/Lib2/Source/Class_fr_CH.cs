using System;
using Epsitec.Localization;

namespace Lib2
{
	public class Class_fr_CH : ILocalizable
	{
		public Class_fr_CH()
		{
			Console.WriteLine (">>>> Lib2.Class_fr_CH");
			Console.WriteLine (this.GetString ("Hello World from Lib2"));
			Console.WriteLine (this.GetString ("Hello Worelede from Lib2"));
			Console.WriteLine ("<<<< Lib2.Class_fr_CH");
			Console.WriteLine ();

			new Lib1.Class_fr_CH ();
		}

		public string Domain => Localizable.DefaultDomain<Class_fr_CH> ();
		public string Culture => "fr-CH";
	}
}
