# Premotion AspNet AppHarbor Integration
*Brought to you by [Premotion Software Solutions](http://www.premotion.nl/ "Premotion Software Solutions")*

This module modifies the native System.Web.HttpContext to make ASP.NET agnostic of the [AppHarbor](https://appharbor.com/ "AppHarbor") load balancing setup. You no longer need to worry about port numbers, secure connections and eaten remote IP addresses.

This library fixes the following issues for you automagically:

* [http://support.appharbor.com/kb/getting-started/workaround-for-generating-absolute-urls-without-port-number](http://support.appharbor.com/kb/getting-started/workaround-for-generating-absolute-urls-without-port-number)
* [http://support.appharbor.com/kb/getting-started/information-about-our-load-balancer](http://support.appharbor.com/kb/getting-started/information-about-our-load-balancer)

## Configuration
To set up the module properly add the following module to your web.config:

	<system.webServer>
		<modules>
			<remove name="AspNetAppHarborIntegration" />
			<add name="AspNetAppHarborIntegration" type="Premotion.AspNet.AppHarbor.Integration.AppHarborIntegrationModule, Premotion.AspNet.AppHarbor.Integration" />
		</modules>
	</system.webServer>

Please note that this might already be done automagically when you installed the library from NuGet.

## Contributors

All help is welcome!

## Copyright

Copyright © 2012 Premotion Software Solutions and contributors.

## License

Premotion AspNet AppHarbor Integration is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to license.txt for more information.

## Download
You can download the compiled binary from NuGet: [http://nuget.org/packages/Premotion.AspNet.AppHarbor.Integration](http://nuget.org/packages/Premotion.AspNet.AppHarbor.Integration)