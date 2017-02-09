#region Copyright © 2005 Paul Welter. All rights reserved.
/*
Copyright © 2005 Paul Welter. All rights reserved.
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:
1. Redistributions of source code must retain the above copyright
	notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
	notice, this list of conditions and the following disclaimer in the
	documentation and/or other materials provided with the distribution.
3. The name of the author may not be used to endorse or promote products
	derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

#region 2011-12-19: Default Encoding by Manuel Sprock (Phoenix Contact)
/* 
Support for default encoding (utf-8-without-bom) added. 
There is no WebName for utf-8-without-bom. It's the default 
encoding when not specifying an encoding and using the overload
File.WriteAllText(fileName, buffer) 
Cf. http://msdn.microsoft.com/en-us/library/ms143375.aspx
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Epsitec.Zou
{
	/// <summary>
	/// Replace text in file(s) using a Regular Expression.
	/// </summary>
	/// <example>Search for a version number and update the revision.
	/// <code><![CDATA[
	/// <FileUpdate Files="version.txt"
	///     Regex="(\d+)\.(\d+)\.(\d+)\.(\d+)"
	///     ReplacementText="$1.$2.$3.123" />
	/// ]]></code>
	/// </example>
	public class FileUpdate : Task
	{
		public ITaskItem[]	Files
		{
			get; set;
		}
		public string		Regex
		{
			get; set;
		}
		public bool			IgnoreCase
		{
			get; set;
		}
		/// <summary>
		/// Gets or sets a value changing the meaning of ^ and $ so they match at the beginning and end, 
		/// respectively, of any line, and not just the beginning and end of the entire string.
		/// </summary>
		/// <value><c>true</c> if multiline; otherwise, <c>false</c>.</value>
		public bool			Multiline
		{
			get; set;
		}
		/// <summary>
		/// Gets or sets a value changing the meaning of the dot (.) so it matches 
		/// every character (instead of every character except \n). 
		/// </summary>
		/// <value><c>true</c> if singleline; otherwise, <c>false</c>.</value>
		public bool			Singleline
		{
			get; set;
		}
		/// <summary>
		/// Gets or sets the maximum number of times the replacement can occur.
		/// </summary>
		/// <value>The replacement count.</value>
		public int			ReplacementCount { get; set; } = -1;
		public bool			ReplacementTextEmpty
		{
			get; set;
		}
		public string		ReplacementText
		{
			get; set;
		}

		/// Maintain the behaviour of the original implementation for compatibility
		/// (i.e. initialize this.useDefaultEncoding with false) and use utf-8-without-bom,  
		/// which is Microsoft's default encoding, only when Encoding property is set 
		/// to "utf-8-without-bom". 

		/// <summary>
		/// The character encoding used to read and write the file.
		/// </summary>
		/// <remarks>Any value returned by <see cref="System.Text.Encoding.WebName"/> is valid input.
		/// <para>The default is <c>utf-8</c></para>
		/// <para>Additionally, <c>utf-8-without-bom</c>can be used.</para></remarks>
		public string		Encoding
		{
			get
			{
				if (this.useDefaultEncoding)
					return "utf-8-without-bom";
				else
					return this.encoding.WebName;
			}
			set
			{
				if (value.ToLower ().CompareTo ("utf-8-without-bom") == 0)
					this.useDefaultEncoding = true;
				else
					this.encoding = System.Text.Encoding.GetEncoding (value);
			}
		}

		//--added for testing

		/// <summary>
		/// When TRUE, a warning will be generated to show which file was not updated.
		/// </summary>
		/// <remarks>N/A</remarks>
		public bool			WarnOnNoUpdate
		{
			get; set;
		}
		/// <summary>
		/// Returns list of items that were not updated
		/// </summary>
		[Output]
		public ITaskItem[]	ItemsNotUpdated
		{
			get; set;
		}
		/// <summary>
		/// Returns true if all items were updated, else false
		/// </summary>
		[Output]
		public bool			AllItemsUpdated { get; set; } = true;
		/// <summary>
		/// Returns true if file changed, else false
		/// </summary>
		[Output]
		public bool			Changed { get; set; }
		/// <summary>
		/// When overridden in a derived class, executes the task.
		/// </summary>
		/// <returns>
		/// true if the task successfully executed; otherwise, false.
		/// </returns>
		public override bool Execute()
		{
			var options = RegexOptions.None;

			if (this.IgnoreCase)
			{
				options |= RegexOptions.IgnoreCase;
			}
			if (this.Multiline)
			{
				options |= RegexOptions.Multiline;
			}
			if (this.Singleline)
			{
				options |= RegexOptions.Singleline;
			}

			if (this.ReplacementCount == 0)
			{
				this.ReplacementCount = -1;
			}

			if (this.ReplacementTextEmpty)
			{
				this.ReplacementText = String.Empty;
			}

			var replaceRegex = new Regex (this.Regex, options);

			try
			{
				var itemsNotUpdated = new List<ITaskItem> ();

				foreach (ITaskItem item in this.Files)
				{
					string fileName = item.ItemSpec;
					Log.LogMessage ("Updating File \"{0}\".", fileName);

					string buffer = "";

					if (this.useDefaultEncoding)
					{
						buffer = File.ReadAllText (fileName);
					}
					else
					{
						buffer = File.ReadAllText (fileName, this.encoding);
					}

					if (!replaceRegex.IsMatch (buffer))
					{
						itemsNotUpdated.Add (item);

						if (this.WarnOnNoUpdate)
						{
							Log.LogWarning (String.Format ("No updates were performed on file : {0}.", fileName));
						}
					}

					var outputBuffer = replaceRegex.Replace (buffer, this.ReplacementText, this.ReplacementCount);
					if (this.Changed = outputBuffer != buffer)
					{
						if (this.useDefaultEncoding)
						{
							File.WriteAllText (fileName, outputBuffer);
						}
						else
						{
							File.WriteAllText (fileName, outputBuffer, this.encoding);
						}
					}
				}

				if (itemsNotUpdated.Count > 0)
				{
					ItemsNotUpdated = itemsNotUpdated.ToArray ();
					AllItemsUpdated = false;
				}
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException (ex);
				AllItemsUpdated = false;
				return false;
			}
			return true;
		}

		private Encoding	encoding = System.Text.Encoding.UTF8;
		private bool		useDefaultEncoding;
	}
}
