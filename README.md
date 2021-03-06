Google Analytics Handler for ASP
================================

An ASP handler that reports request information to Google Analytics.

Since Google Analytics is operated by the client, embedded as JavaScript
in HTML, it will only track HTML requests.  By adding this Handler in
front of non-HTML files, you can send the request information directly
from the server, as part of delivering the content.

This allows you to use Google Analytics to track other requests, like
media downloads (audio and video), plain text files, or XML (including
RSS and Atom).

You can configure this handler in an on-premises IIS installation, or
inside an Azure Web App.

Instructions
------------

1. Add the handler to your web application.  Simply add the
   `GoogleAnalyticsHandler.dll` to the `bin` directory of your web app.

2. Add your Google Analytics Property ID as an `appSetting` for your
   web app.  You can configure this in your `Web.config`:

     <configuration>
       <appSettings>
         <add key="googleanalytics_id" value="UA-1234567-1" />
       </appSettings>
     </configuration>

   You can also set this as a connection string, which is useful in case
   your `appSetting`s are not inherited in a way that you would like,
   particularly if you are using the location-based redirect appSetting
   configuration described below.

   For Azure Web Apps, you can configure this in the Azure Portal.

3. Configure the handler for whatever path(s) you want to report to
   Google Analytics.  For example, to configure all `rss.xml` requests
   to be recorded to Google Analytics, add this to your `Web.config`:

     <configuration>
       <system.webServer>
         <handlers>
           <add name="RssGoogleAnalyticsHandler"
                verb="*"
                path="rss.xml"
                type="GoogleAnalyticsHandler.GoogleAnalyticsHandler, GoogleAnalyticsHandler"
                resourceType="Unspecified" />
         </handlers>
       </system.webServer>
     </configuration>

   You can also use wildcards here: for example, setting `path="old/*"` will
   send all requests beneath the `old/` directory to this handler.

4. (Optional) If you do not want to deliver a local file, but want to
   perform a redirect (while delivering request information to Google
   Analytics), then configure the redirect path as an application setting.
   You can configure this in your `Web.config`:   

     <configuration>
       <location path="old/foo.html">
         <appSettings>
           <add key="redirect" value="https://www.example.com/foo.html" />
         </appSettings>
       </location>
     </configuration>

   Now any request for `/old/foo.html` will be redirected to
   `https://www.example.com/foo.html` and Google Analytics will be notified
   about the request for `/old/foo.html`.

5. (Optional) If you want to redirect an entire directory, such that any
   request within a directory is redirected to a different directory, you
   can configure this in your `Web.config`:

     <configuration>
       <location path="old">
         <appSettings>
           <add key="redirect-root" value="https://www.example.com/" />
         </appSettings>
       </location>
     </configuration>

    Now any request for `/old/bar.html` will be redirected to
    `https://www.example.com/bar.html` and Google Analytics will be notified
    about the request for `/old/bar.html`.  Similarly, any request beneath
    the `old/` directory will be handled in the same manner.

