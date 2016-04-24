using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epsitec.GetText;
using NGettext;

namespace Console1
{
	class Program
	{
		static void Main(string[] args)
		{
			//Console.WriteLine (t._ ("Hello World from Console1!"));
			Console.WriteLine (catalog.GetString ("Hello World from Console1!"));
		}
		//private static readonly T t = new T("Console1");
		private static readonly ICatalog catalog = AppDomain.CurrentDomain.CreateCatalog("Console1");
	}
}
