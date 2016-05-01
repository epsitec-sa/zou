using System;

using Epsitec.Localization;
using NGettext;

namespace Lib1
{
    public class Class_fr_CH : ILocalizable
    {
		public Class_fr_CH()
		{
			Console.WriteLine (">>>> Lib1.Class_fr_CH");
			Console.WriteLine(this.GetString ("Hello World from Lib1"));
			Console.WriteLine(this.GetString ("Hello Worelede from Lib1"));
			Console.WriteLine ("<<<< Lib1.Class_fr_CH");
			Console.WriteLine ();
		}

		public string Domain  => Localizable.DefaultDomain<Class_fr_CH> ();
		public string Culture => "fr-CH";
	}
}
