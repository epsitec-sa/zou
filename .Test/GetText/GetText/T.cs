using System;
using System.Globalization;
using System.IO;
using NGettext;

namespace Epsitec.GetText
{
	public class T
	{
		public T(string domain)
			: this(domain, T.LocaleDir, CultureInfo.CurrentUICulture)
		{
		}
		public T(string domain, CultureInfo cultureInfo)
			: this(domain, T.LocaleDir, cultureInfo)
		{
		}
		public T(string domain, string culture)
			: this(domain, T.LocaleDir, culture)
		{
		}
		public T(string domain, string localeDir, string culture)
			: this(domain, localeDir, CultureInfo.CreateSpecificCulture (culture))
		{
		}
		public T(string domain, string localeDir, CultureInfo cultureInfo)
		{
			this.catalog = new Catalog (domain, localeDir, cultureInfo);
		}

		public string _(string text)
		{
			return this.catalog.GetString (text);
		}
		public string _(string text, params object[] args)
		{
			return this.catalog.GetString (text, args);
		}
		public string _n(string text, string pluralText, long n)
		{
			return this.catalog.GetPluralString (text, pluralText, n);
		}
		public string _n(string text, string pluralText, long n, params object[] args)
		{
			return this.catalog.GetPluralString (text, pluralText, n, args);
		}
		public string _p(string context, string text)
		{
			return this.catalog.GetParticularString (context, text);
		}
		public string _p(string context, string text, params object[] args)
		{
			return this.catalog.GetParticularString (context, text, args);
		}
		public string _pn(string context, string text, string pluralText, long n)
		{
			return this.catalog.GetParticularPluralString (context, text, pluralText, n);
		}
		public string _pn(string context, string text, string pluralText, long n, params object[] args)
		{
			return this.catalog.GetParticularPluralString (context, text, pluralText, n, args);
		}

		private static string LocaleDir => Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "locale");

		private readonly ICatalog catalog;
	}
}
