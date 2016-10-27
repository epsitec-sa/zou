using System;
using System.Globalization;
using System.Threading;
using Epsitec.Localization;
using NGettext;

namespace Console1
{
	class Program : ILocalizable<Program>
	{
		static readonly string Culture = "de-CH";

		static Program()
		{
			// Set the defined default culture process wide.
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture (Program.Culture);

			Console.WriteLine ($"CurrentUICulture = {Program.Culture}");
			Console.WriteLine ("========================");
			Console.WriteLine ();
		}

		static void Main(string[] args)
		{
			Console.WriteLine (Program.Catalog.GetString ("Hello World from Console1!"));
			Console.WriteLine (Program.Catalog.GetString ("Goodbye from Console1!"));
			Console.WriteLine ();

			new Lib2.Class_fr_CH ();
			new Lib2.Class_CurrentUICulture ();
		}

		static readonly ICatalog Catalog = CatalogRepository.Default.GetCatalog<Program>(Program.Culture);
	}
}
