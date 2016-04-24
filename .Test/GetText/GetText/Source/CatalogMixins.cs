using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGettext;

namespace Epsitec.GetText
{
	public static class CatalogMixins
	{
		public static ICatalog CreateCatalog(this AppDomain self, string domain)
		{
			return self.CreateCatalog (domain, CultureInfo.CurrentUICulture);
		}
		public static ICatalog CreateCatalog(this AppDomain self, string domain, string culture)
		{
			return self.CreateCatalog (domain, CultureInfo.CreateSpecificCulture (culture));
		}
		public static ICatalog CreateCatalog(this AppDomain self, string domain, CultureInfo cultureInfo)
		{
			var localeDir = Path.Combine (self.BaseDirectory, "locale");
			return new Catalog (domain, localeDir, cultureInfo);
		}
	}
}
