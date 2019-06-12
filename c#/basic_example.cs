using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Drawing;
using System.Net.Http;
 
namespace ODataFileTransfer
{
    class ODataFileTransfer
    {
        private const string serverUri = "https://014246-studio.bpmonline.com/0/ServiceModel/EntityDataService.svc/";
        private const string authServiceUri = "https://014246-studio.bpmonline.com/ServiceModel/AuthService.svc/Login";
        private const string userName = "Supervisor";
        private const string userPassword = "Supervisor";
 
        private static readonly XNamespace ds = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace dsmd = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace atom = "http://www.w3.org/2005/Atom";
 
        public static CookieContainer AuthCookie = new CookieContainer();
        private static string CsrfToken = "";
        private static int LoginAttempts = 0;
 
        static void Main(string[] args)
        {
            GetOdataCollectionByAuthByHttpExample();
        }
 
        static void TryLogin()
        {
            var authRequest = HttpWebRequest.Create(authServiceUri) as HttpWebRequest;
            authRequest.Method = "POST";
            authRequest.ContentType = "application/json";
            authRequest.CookieContainer = AuthCookie;
            using (var requestStream = authRequest.GetRequestStream())
            {
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{ ""UserName"":""" + userName + @""", ""UserPassword"":""" + userPassword + @""", ""SolutionName"":""TSBpm"", ""TimeZoneOffset"":-120, ""Language"":""En-us"" }");
                }
            }
            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                CsrfToken = cookieCollection["BPMCSRF"].Value;
 
            }
        }
 
        static HttpWebResponse SendRequest(string requestURIstring, string method = "GET", XElement entry = null)
        {
            var dataRequest = HttpWebRequest.Create(requestURIstring) as HttpWebRequest;
            dataRequest.Method = method;
            dataRequest.CookieContainer = AuthCookie;
            dataRequest.Accept = "application/atom+xml";
            dataRequest.ContentType = "application/atom+xml;type=entry";
            dataRequest.Headers.Add("BPMCSRF", CsrfToken);
 
            if (entry!= null)
            {
                using (var writer = XmlWriter.Create(dataRequest.GetRequestStream()))
                {
                    entry.WriteTo(writer);
                }
            }
 
            try
            {
                var dataResponse = (HttpWebResponse)dataRequest.GetResponse();
                LoginAttempts = 0;
                return dataResponse;
            }
            catch (WebException ex)
            {
                var webResponse = (HttpWebResponse)ex.Response;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
 
                    TryLogin();
                    if(LoginAttempts < 3)
                    {
                        LoginAttempts += 1;
                        SendRequest(requestURIstring);
                    }
                    else
                    {
                        //here can be some handler or logger
                    }
                }
 
            }
            return null;
        }
 
        public static void GetOdataCollectionByAuthByHttpExample()
        {
            var uri = serverUri + "ContactCollection?select=Id, Name";
            using (var dataResponse = SendRequest(uri))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                    ProcessCollection(xmlDoc);
                }
            }
 
            using (var dataResponse = SendRequest(uri))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                    ProcessCollection(xmlDoc);
                }
            }
 
            var content = new XElement(dsmd + "properties",
                  new XElement(ds + "Name", "Jhon Gilts"),
                  new XElement(ds + "Dear", "Jhon"));
            var entry = new XElement(atom + "entry",
                        new XElement(atom + "content",
                        new XAttribute("type", "application/xml"), content));
 
            var uriForPost = serverUri + "ContactCollection/";
 
            using (var dataResponse = SendRequest(uriForPost, "POST", entry))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                }
            }
        }
 
        public static void ProcessCollection(XDocument xmlDoc)
        {
            var contacts = from entry in xmlDoc.Descendants(atom + "entry")
                           select new
                           {
                               Id = new Guid(entry.Element(atom + "content")
                                                       .Element(dsmd + "properties")
                                                       .Element(ds + "Id").Value),
                               Name = entry.Element(atom + "content")
                                               .Element(dsmd + "properties")
                                               .Element(ds + "Name").Value
                           };
            foreach (var contact in contacts)
            {
                // Implementing actions with contacts.
            }
        }
    }
}