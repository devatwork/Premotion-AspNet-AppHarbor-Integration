using System;
using System.Web;

namespace Premotion.AspNet.AppHarbor.Integration.TestWebsite
{
	/// <summary>
	/// Throws an exception on purpose.
	/// </summary>
	public class IntentionalExceptionThrowerHandler : IHttpHandler
	{
		#region Implementation of IHttpHandler
		/// <summary>
		/// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests. </param>
		public void ProcessRequest( HttpContext context )
		{
			throw new Exception( "This exception is thrown on purpose" );
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