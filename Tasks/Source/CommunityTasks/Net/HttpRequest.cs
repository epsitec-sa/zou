// https://github.com/loresoft/msbuildtasks/blob/36e644be15b367ef53cb82540bd1e6d5525ab419/Source/MSBuild.Community.Tasks/Net/HttpRequest.cs

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Net;

namespace Zou.Tasks
{
    /// <summary>
    /// Makes an HTTP request, optionally validating the result and writing it to a file.
    /// </summary>
    /// <remarks>
    /// Execute a http request to hit the database update.  
    /// Target attributes to set:
    ///     Url (required),
    ///     FailOnNon2xxResponse (200 responses generally means successful http request. default=true), 
    ///     EnsureResponseContains (string to check for),
    ///     WriteResponseTo (file name to write to),
    /// </remarks>
    /// <example>
    /// Example of a update request ensuring "Database upgrade check completed successfully." was returned.
    /// <code><![CDATA[
    ///     <HttpRequest Url="http://mydomain.com/index.php?checkdb=1" 
    ///         EnsureResponseContains="Database upgrade check completed successfully." 
    ///         FailOnNon2xxResponse="true" />
    /// ]]></code>
    /// </example>
    public class HttpRequest : Task
    {
        /// <summary>
        /// The URL to make an HTTP request against.
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// Optional: if set then the task fails if the response text doesn't contain the text specified.
        /// </summary>
        public string EnsureResponseContains { get; set; }

        /// <summary>
        /// Default is true.  When true, if the web server returns a status code less than 200 or greater than 299 then the task fails.
        /// </summary>
        public bool FailOnNon2xxResponse { get; set; }

        /// <summary>
        /// Optional, default is GET. The HTTP method to use for the request.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Optional. The username to use with basic authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Optional. The password to use with basic authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Optional; the name of the file to reqd the request from.
        /// </summary>
        public string ReadRequestFrom { get; set; }

        /// <summary>
        /// Optional; the name of the file to write the response to.
        /// </summary>
        public string WriteResponseTo { get; set; }

        /// <summary>
        /// Constructor to set the default parameters for http request
        /// </summary>
        public HttpRequest()
            : base()
        {
            this.FailOnNon2xxResponse = true;
            this.EnsureResponseContains = null;
        }

        /// <summary>
        /// Entry Point inherited from Task
        /// </summary>
        public override bool Execute()
        {
            this.Log.LogMessage("Requesting {0}", this.Url);

            if (!(WebRequest.Create(this.Url) is HttpWebRequest request))
            {
                this.Log.LogError("Url \"{0}\" did not create an HttpRequest.", this.Url);
                return false;
            }

            if (!string.IsNullOrEmpty(this.Method))
            {
                request.Method = this.Method;
            }

            if (!string.IsNullOrEmpty(this.Username) && !string.IsNullOrEmpty(this.Password))
            {
                request.Credentials = new NetworkCredential(this.Username, this.Password);
            }

            if (this.ReadRequestFromFile)
            {
                request.SendChunked = true;
                using (var source = File.Open(this.ReadRequestFrom, FileMode.Open))
                {
                    using (var requestStream = request.GetRequestStream())
                    {
                        byte[] buffer = new byte[16 * 1024];
                        int bytesRead;
                        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                var code = (int)response.StatusCode;
                this.Log.LogMessage("HTTP RESPONSE: {0}, {1}", code, response.StatusDescription);

                if (this.FailOnNon2xxResponse)
                {
                    if (code < 200 || code > 299)
                    {
                        this.Log.LogError("Status code not in Successful 2xx range.");
                        return false;
                    }
                }
                if (this.CheckResponseContents || this.WriteResponseToFile)
                {
                    var responseReader = new StreamReader(response.GetResponseStream());
                    var responseString = responseReader.ReadToEnd();
                    if (this.WriteResponseToFile)
                    {
                        using (var writer = new StreamWriter(this.WriteResponseTo))
                        {
                            writer.Write(responseString);
                            writer.Close();
                        }
                    }
                    if (this.CheckResponseContents)
                    {
                        if (!responseString.Contains(this.EnsureResponseContains))
                        {
                            var length = System.Math.Min(100, responseString.Length);
                            this.Log.LogError("Response did not contain the specified text.  Started with: " + responseString.Substring(0, length));
                            return false;
                        }
                    }
                }
                response.Close();
            }
            return true;
        }

        private bool CheckResponseContents => !string.IsNullOrEmpty(this.EnsureResponseContains);
        private bool ReadRequestFromFile => !string.IsNullOrEmpty(this.ReadRequestFrom);
        private bool WriteResponseToFile => !string.IsNullOrEmpty(this.WriteResponseTo);
    }
}