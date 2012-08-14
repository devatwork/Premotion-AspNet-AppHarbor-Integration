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

			                        	// split the forwarded for header by comma+space separated list of IP addresses, the left-most being the farthest downstream client, in order to set the correct REMOTE_ADDR
			                        	// see http://en.wikipedia.org/wiki/X-Forwarded-For
			                        	// seealso: https://github.com/trilobyte/Premotion-AspNet-AppHarbor-Integration/issues/6
			                        	var forwardedFor = serverVariables[ForwardedForHeaderName] ?? string.Empty;
			                        	if (!string.IsNullOrEmpty(forwardedFor))
			                        	{
			                        		var forwardSeparatorIndex = forwardedFor.LastIndexOf(ForwardedForAddressesSeparator);

			                        		// if there is only one result, the HTTP_X_FORWARDED_FOR contains only the client IP
			                        		if (forwardSeparatorIndex < 0)
			                        		{
			                        			// there is only address in the header which is the REMOTE_ADDR
			                        			serverVariables.Set("REMOTE_ADDR", forwardedFor);

			                        			// remove the HTTP_X_FORWARDED_FOR header because it is set by the AppHarbor loadbalancer
			                        			serverVariables.Remove(ForwardedForHeaderName);
			                        		}
			                        		else
			                        		{
			                        			// use the right-most address as the REMOTE_ADDR, this is how any other non load-balanced web server would normally see it
			                        			serverVariables.Set("REMOTE_ADDR", forwardedFor.Substring(forwardSeparatorIndex + ForwardedForAddressesSeparator.Length));

			                        			// remove the last value from the HTTP_X_FORWARDED_FOR header, this value is added by the AppHarbor loadbalancer
			                        			serverVariables.Set(ForwardedForHeaderName, forwardedFor.Remove(forwardSeparatorIndex));
			                        		}
			                        	}

			                        	// get the original protocol and remove the header added by the AppHarbor loadbalancer
			                        	var protocol = serverVariables[ForwardedProtocolHeaderName];
			                        	if (!string.IsNullOrEmpty(protocol))
			                        	{
			                        		serverVariables.Remove(ForwardedProtocolHeaderName);

			                        		// fix the port and protocol
			                        		var isHttps = "HTTPS".Equals(protocol, StringComparison.OrdinalIgnoreCase);
			                        		serverVariables.Set("HTTPS", isHttps ? "on" : "off");
			                        		serverVariables.Set("SERVER_PORT", isHttps ? "443" : "80");
			                        		serverVariables.Set("SERVER_PORT_SECURE", isHttps ? "1" : "0");
			                        	}

			                        	// Get original IsAjax request header and attach to request.
			                        	// see #8 for details
			                        	var isAjaxFlag = serverVariables["HTTP_X_REQUESTED_WITH"];
			                        	if (!string.IsNullOrEmpty(isAjaxFlag))
			                        		serverVariables.Set("X-Requested-With", isAjaxFlag);

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