using System;
using System.Globalization;
using Epsitec.Localization;
using NGettext;

namespace Lib1
{
    public class Class_fr_CH : Localizable
    {
		public Class_fr_CH()
		{
			Console.WriteLine (">>>> Lib1.Class_fr_CH");
			Console.WriteLine(this.GetString ("Hello World from Lib1"));
			Console.WriteLine(this.GetString ("Hello Worelede from Lib1"));
			Console.WriteLine ("<<<< Lib1.Class_fr_CH");
			Console.WriteLine ();
		}

		public override CultureInfo Culture => CultureInfo.CreateSpecificCulture("fr-CH");
	}
}
