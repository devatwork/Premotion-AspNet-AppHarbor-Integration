using System.Web;

namespace Premotion.AspNet.AppHarbor.Integration.TestWebsite
{
	/// <summary>
	/// Handler which makes a plain text dump of the server variables and some request variables.
	/// </summary>
	public class RequestDetailsHandler : IHttpHandler
	{
		#region Implementation of IHttpHandler
		/// <summary>
		/// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests. </param>
		public void ProcessRequest(HttpContext context)
		{
			// get the response
			var request = context.Request;
			var response = context.Response;
			var output = response.Output;

			// set the mime type
			response.ContentType = "text/plain";

			// write out some request details
			output.WriteLine("### Request variables. look ma, my no special ports and my remote IP");
			output.WriteLine("Request.Url: " + request.Url);
			output.WriteLine("Request.IsSecureConnection: " + request.IsSecureConnection);
			output.WriteLine("Request.UserHostAddress: " + request.UserHostAddress);
			output.WriteLine("Request.Path: " + request.Path);
			output.WriteLine("Request.PathInfo: " + request.PathInfo);

			output.WriteLine();
			output.WriteLine();

			// dump server variables
			output.WriteLine("### Server variables");
			var serverVariables = request.ServerVariables;
			foreach (var key in serverVariables.AllKeys)
			   output.WriteLine(string.Format("{0}: {1}", key, serverVariables[key]));

			// dump request headers
			output.WriteLine("### Request headers");
			var headerVariables = request.Headers;
			foreach (var key in headerVariables.AllKeys)
			   output.WriteLine(string.Format("{0}: {1}", key, headerVariables[key]));

			// dump query string
			output.WriteLine("### Querystring (GET)");
			var queryStringVariables = request.QueryString;
			foreach (var key in queryStringVariables.AllKeys)
			   output.WriteLine(string.Format("{0}: {1}", key, queryStringVariables[key]));

			// dump POST
			output.WriteLine("### Form variables (POST)");
			var formVariables = request.Form;
			foreach (var key in formVariables.AllKeys)
			   output.WriteLine(string.Format("{0}: {1}", key, formVariables[key]));

			// dump Session
			output.WriteLine("### Session");
			var session = context.Session;
			if (session == null)
			   output.WriteLine("No session");
			else
			{
			   var count = session.Count;
			   output.WriteLine("Count: {0}\r\n", count);
			   foreach (string key in session.Keys)
			      output.WriteLine(string.Format("{0}: {1}", key, session[key]));
			}

			// dump cookies
			output.WriteLine("### Cookies");
			var cookieCollection = request.Cookies;
			foreach (var key in cookieCollection.AllKeys)
			   output.WriteLine(string.Format("{0}: {1}", key, cookieCollection[key]));
		}
		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
		/// </returns>
		public bool IsReusable
		{
			get { return true; }
		}
		#endregion
	}
}