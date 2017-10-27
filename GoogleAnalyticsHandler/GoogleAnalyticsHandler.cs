using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GoogleAnalyticsHandler
{
    public class GoogleAnalyticsHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            NameValueCollection settings = ConfigurationManager.AppSettings;

            SendTracking(context);

            /* If the redirect app setting is set, that takes priority. */
            if (settings.Get("redirect") != null ||
                settings.Get("redirect-root") != null)
            {
                ProcessRedirect(context);
            }
            /* Otherwise, try to deliver a file */
            else
            {
                ProcessFile(context);
            }
        }

        private void ProcessRedirect(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            NameValueCollection settings = ConfigurationManager.AppSettings;
            string redirectPath = ConfigurationManager.AppSettings.Get("redirect");

            if (redirectPath == null)
            {
                string redirectRoot = settings.Get("redirect-root");
                redirectPath = new Uri(new Uri(redirectRoot), request.Url.Segments.Last()).ToString();
            }

            redirectPath = Uri.EscapeUriString(redirectPath);

            response.Clear();
            response.StatusCode = 302;
            response.StatusDescription = "Moved Temporarily";
            response.AddHeader("Location", redirectPath);
            response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.AddHeader("Pragma", "no-cache");
            response.AddHeader("Expires", "0");
        }

        private void ProcessFile(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            NameValueCollection settings = ConfigurationManager.AppSettings;

            string contentType = MimeMapping.GetMimeMapping(request.Url.Segments.Last());
            string localPath = settings.Get("file") ?? request.Path;

            if (contentType == null)
            {
                contentType = "application/octet-stream";
            }

            response.Clear();
            response.AddHeader("Content-Type", contentType);
            response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.AddHeader("Pragma", "no-cache");
            response.AddHeader("Expires", "0");
            response.WriteFile(localPath);
        }

        private void SendTracking(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            NameValueCollection settings = ConfigurationManager.AppSettings;

            string propertyId = ConfigurationManager.ConnectionStrings["googleanalytics_id"].ConnectionString;

            if (propertyId == null)
            {
                propertyId = settings.Get("googleanalytics_id");
            }

            if (String.IsNullOrEmpty(propertyId))
            {
                throw new Exception("The Google Analytics property ID must be specified");
            }

            /* Send the GA tracking information */
            WebRequest trackingRequest = WebRequest.Create("http://www.google-analytics.com/collect");

            /* Returning visitors have a clientid set by Google Analytics, in
             * the GA cookie.  Try to use that. */
            HttpCookie gaCookie = context.Request.Cookies["_ga"];
            string clientId;

            if (gaCookie != null && gaCookie.Value != null &&
                gaCookie.Value.StartsWith("GA1.2."))
            {
                clientId = gaCookie.Value.Substring(6);
            }
            else
            {
                clientId = Guid.NewGuid().ToString();
            }

            string remoteHost = context.Request.Url.Host ?? "";
            string remotePath = context.Request.Url.PathAndQuery ?? "/";
            string referrer = context.Request.UrlReferrer != null ?
                context.Request.UrlReferrer.ToString() : "";
            string userHost = context.Request.UserHostAddress ?? "";
            string userAgent = context.Request.UserAgent ?? "";

            string postData = String.Format("v=1&t={0}&tid={1}&cid={2}&dh={3}&dp={4}&dr={5}&uip={6}&ua={7}",
                Uri.EscapeDataString("pageview"),
                Uri.EscapeDataString(propertyId),
                Uri.EscapeDataString(clientId),
                Uri.EscapeDataString(remoteHost),
                Uri.EscapeDataString(remotePath),
                Uri.EscapeDataString(referrer),
                Uri.EscapeDataString(userHost),
                Uri.EscapeDataString(userAgent));
            byte[] data = Encoding.ASCII.GetBytes(postData);

            trackingRequest.Method = "POST";
            trackingRequest.ContentType = "application/x-www-form-urlencoded";
            trackingRequest.ContentLength = data.Length;

            using (Stream stream = trackingRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            new StreamReader(trackingRequest.GetResponse().GetResponseStream()).ReadToEnd();
        }
    }
}
