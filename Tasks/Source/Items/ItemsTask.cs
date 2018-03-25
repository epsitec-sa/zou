using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public abstract class ItemsTask : Task
	{
		[Required]
		public string File
		{
			get;
			set;
		}
		public string RelativeTo
		{
			get;
			set;
		}

		protected string[] ReadAllLines()
		{
			return System.IO.File.Exists (this.File)
				? System.IO.File.ReadAllLines (this.File)
				: new string[0];
		}
	}
}
