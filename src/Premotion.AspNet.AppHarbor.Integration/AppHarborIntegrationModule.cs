using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace Premotion.AspNet.AppHarbor.Integration
{
	/// <summary>
	/// This module modifies the native <see cref="T:System.Web.HttpContext"/> to hide the AppHarbor load balancing setup from ASP.Net.
	/// </summary>
	/// <remarks>
	/// This should take care of the following issues:
	/// http://support.appharbor.com/kb/getting-started/workaround-for-generating-absolute-urls-without-port-number
	/// http://support.appharbor.com/kb/getting-started/information-about-our-load-balancer
	/// </remarks>
	public class AppHarborModule : IHttpModule
	{
		#region Constants
		/// <summary>
		/// Defines the name of the setting which to use to detect AppHarbor.
		/// </summary>
		private const string AppHarborDetectionSettingKey = "appharbor.commit_id";
		/// <summary>
		/// AppHarbor uses a load balancer which rewrites the REMOTE_ADDR header. 
		/// The original user's IP addres is stored in a separate header with this name.
		/// </summary>
		private const string ForwardedForHeaderName = "HTTP_X_FORWARDED_FOR";
		/// <summary>
		/// AppHarbor uses a load balancer which rewrites the SERVER_PROTOCOL header. 
		/// The original protocol is stored in a separate header with this name.
		/// </summary>
		/// <remarks>http://en.wikipedia.org/wiki/X-Forwarded-For</remarks>
		private const string ForwardedProtocolHeaderName = "HTTP_X_FORWARDED_PROTO";
		/// <summary>
		/// Defines the separator which to use to split the Forwarded for header.
		/// </summary>
		/// <remarks>http://en.wikipedia.org/wiki/X-Forwarded-For</remarks>
		private const string ForwardedForAddressesSeparator = ", ";
		#endregion
		#region Implementation of IHttpModule
		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application </param>
		public void Init(HttpApplication context)
		{
			//If we're not running on AppHarbor, do nothing.
			var appHarborCommitId = ConfigurationManager.AppSettings[AppHarborDetectionSettingKey];
			if (string.IsNullOrEmpty(appHarborCommitId))
				return;

			var collectionType = typeof (NameValueCollection);
			var readOnlyProperty = collectionType.GetProperty("IsReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
			if (readOnlyProperty == null)
				throw new InvalidOperationException(string.Format("Could not find property '{0}' on type '{1}'", "IsReadOnly", collectionType));

			var collectionParam = Expression.Parameter(typeof (NameValueCollection));

			var isReadOnly = Expression.Lambda<Func<NameValueCollection, bool>>(
				Expression.Property(collectionParam, readOnlyProperty),
				collectionParam
				).Compile();

			var valueParam = Expression.Parameter(typeof (bool));
			var setReadOnly = Expression.Lambda<Action<NameValueCollection, bool>>(
				Expression.Call(collectionParam, readOnlyProperty.GetSetMethod(true), valueParam),
				collectionParam, valueParam
				).Compile();

			// listen to incoming requests to modify
			context.BeginRequest += (sender, args) =>
			                        {
			                        	// get the http context
			                        	var serverVariables = HttpContext.Current.Request.ServerVariables;

			                        	// only unlock the collection if it was locked, otherwise an exception will be raised
			                        	// see #5 for details
			                        	var wasReadOnly = isReadOnly(serverVariables);
			                        	if (wasReadOnly)
			                        		setReadOnly(serverVariables, false);

			                        	var forwardedFor = serverVariables[ForwardedForHeaderName] ?? string.Empty;
			                        	if (!string.IsNullOrEmpty(forwardedFor))
			                        	{
			                        		// split the forwarded for header by comma+space separated list of IP addresses, the left-most being the farthest downstream client
			                        		// if the string only contains one IP use that IP
			                        		// see http://en.wikipedia.org/wiki/X-Forwarded-For
			                        		var forwardSeparatorIndex = forwardedFor.IndexOf(ForwardedForAddressesSeparator, StringComparison.OrdinalIgnoreCase);
			                        		serverVariables.Set("REMOTE_ADDR", forwardSeparatorIndex > 0 ? forwardedFor.Remove(forwardSeparatorIndex) : forwardedFor);
			                        	}

			                        	// set correct headers and 
			                        	var protocol = serverVariables[ForwardedProtocolHeaderName] ?? string.Empty;
			                        	var isHttps = "HTTPS".Equals(protocol, StringComparison.OrdinalIgnoreCase);
			                        	serverVariables.Set("HTTPS", isHttps ? "on" : "off");
			                        	serverVariables.Set("SERVER_PORT", isHttps ? "443" : "80");
			                        	serverVariables.Set("SERVER_PORT_SECURE", isHttps ? "1" : "0");

			                        	// only lock the collection if it was previously locked
			                        	// see #5 for details
			                        	if (wasReadOnly)
			                        		setReadOnly(serverVariables, true);
			                        };
		}
		/// <summary>
		/// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
		/// </summary>
		public void Dispose()
		{
			// nothing to do here
		}
		#endregion
	}
}