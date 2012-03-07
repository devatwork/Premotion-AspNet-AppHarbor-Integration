using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace Premotion.AspNet.AppHarbor.Integration
{
	/// <summary>
	/// This module modifies the native <see cref="T:System.Web.HttpContext"/> to make ASP.NET agnostic of the AppHarbor loadbalancing setup.
	/// This module makes it easier to run 
	/// </summary>
	/// <remarks>
	/// This should take care of the following issues:
	/// http://support.appharbor.com/kb/getting-started/workaround-for-generating-absolute-urls-without-port-number
	/// http://support.appharbor.com/kb/getting-started/information-about-our-load-balancer
	/// </remarks>
	public class AppHarborIntegrationModule : IHttpModule
	{
		#region Constants
		/// <summary>
		/// Defines the name of the module enabled setting.
		/// </summary>
		private const string ModuleEnabledSettingKey = "DOCKED_AT_APPHARBOR";
		/// <summary>
		/// AppHarbor uses an loadbalancer which rewrites the REMOTE_ADDR header. The original user's IP addres is stored in a separate header with this name.
		/// </summary>
		private const string ForwardedUserHostAddressHeaderName = "HTTP_X_FORWARDED_FOR";
		/// <summary>
		/// AppHarbor uses an loadbalancer which rewrites the SERVER_PROTOCOL header. The original protocol is stored in a separate header with this name.
		/// </summary>
		private const string ForwardedProtocolHeaderName = "HTTP_X_FORWARDED_PROTO";
		#endregion
		#region Implementation of IHttpModule
		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application </param>
		public void Init(HttpApplication context)
		{
			// check if the library is enabled or not
			var isEnabled = ConfigurationManager.AppSettings[ModuleEnabledSettingKey];
			if (!"true".Equals(isEnabled, StringComparison.OrdinalIgnoreCase))
				return;

			// find the server variables collection accessor methods
			const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
			var serverVariablesCollectionType = Type.GetType("System.Web.HttpServerVarsCollection, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
			if (serverVariablesCollectionType == null)
				throw new InvalidOperationException("Could not find type 'System.Web.HttpServerVarsCollection, System.Web'");
			var makeReadWrite = serverVariablesCollectionType.GetMethod("MakeReadWrite", bindingFlags);
			if (makeReadWrite == null)
				throw new InvalidOperationException(string.Format("Could not find method '{0}' on type '{1}'", "MakeReadWrite", serverVariablesCollectionType));
			var addStatic = serverVariablesCollectionType.GetMethod("Set", bindingFlags);
			if (addStatic == null)
				throw new InvalidOperationException(string.Format("Could not find method '{0}' on type '{1}'", "AddStatic", serverVariablesCollectionType));
			var makeReadOnly = serverVariablesCollectionType.GetMethod("MakeReadOnly", bindingFlags);
			if (makeReadOnly == null)
				throw new InvalidOperationException(string.Format("Could not find method '{0}' on type '{1}'", "MakeReadOnly", serverVariablesCollectionType));

			// create an expression which allows access to the HttpServerVarsCollection class without using reflection, this is almost as fast as native calls.
			var nameParameter = Expression.Parameter(typeof (string), "name");
			var valueParameter = Expression.Parameter(typeof (string), "value");
			var serverVariablesParameter = Expression.Parameter(typeof (NameValueCollection), "serverVariables");
			var instanceExpression = Expression.Convert(serverVariablesParameter, serverVariablesCollectionType);
			var makeReadWriteExpression = Expression.Call(instanceExpression, makeReadWrite);
			var addStaticExpression = Expression.Call(instanceExpression, addStatic, nameParameter, valueParameter);
			var makeReadOnlyExpression = Expression.Call(instanceExpression, makeReadOnly);
			var body = Expression.Block(makeReadWriteExpression, addStaticExpression, makeReadOnlyExpression);
			var setServerVariable = Expression.Lambda<Action<NameValueCollection, string, string>>(body, serverVariablesParameter, nameParameter, valueParameter).Compile();

			// listen to incoming requests to  modify
			context.BeginRequest += (sender, args) =>
			                        {
			                        	// get the http context
			                        	var serverVariables = HttpContext.Current.Request.ServerVariables;

			                        	// get the forwarder headers
			                        	var userHostAddress = serverVariables[ForwardedUserHostAddressHeaderName];
			                        	var protocol = serverVariables[ForwardedProtocolHeaderName];
			                        	var isHttps = "HTTPS".Equals(protocol, StringComparison.OrdinalIgnoreCase);

			                        	// set correct headers
			                        	setServerVariable(serverVariables, "REMOTE_ADDR", userHostAddress);
			                        	setServerVariable(serverVariables, "HTTPS", isHttps ? "on" : "off");
			                        	setServerVariable(serverVariables, "SERVER_PORT", isHttps ? "443" : "80");
			                        	setServerVariable(serverVariables, "SERVER_PORT_SECURE", isHttps ? "1" : "0");
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