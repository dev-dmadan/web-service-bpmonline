using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization.Formatters.Binary;
 
namespace ODataFile
{
    class ODataFileHelper
    {
        public string serverUri = "https://014246-studio.bpmonline.com/0/ServiceModel/EntityDataService.svc/";
        public string authServiceUri = "https://014246-studio.bpmonline.com/ServiceModel/AuthService.svc/Login";
        public string userName = "Supervisor";
        public string userPassword = "Supervisor";
        public int MaxLoginAttempts = 3;
        public string CookiesFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "cookies.dat");
 
        public readonly XNamespace ds = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        public readonly XNamespace dsmd = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        public readonly XNamespace atom = "http://www.w3.org/2005/Atom";
 
        CookieContainer AuthCookie = new CookieContainer();
        string CsrfToken = "";
        int LoginAttempts = 0;        
 
        public HttpWebResponse SendRequest(string requestURIstring, string method = "GET", XElement entry = null)
        {
            ReadCookiesFromDisk();
            while (LoginAttempts < MaxLoginAttempts)
            {
                HttpWebRequest dataRequest = BuildRequest(requestURIstring, method, entry);
                dataRequest.CookieContainer = AuthCookie;
                dataRequest.Headers.Add("BPMCSRF", CsrfToken);
                try
                {
                    var dataResponse = (HttpWebResponse)dataRequest.GetResponse();
                    WriteCookiesToDisk();
                    LoginAttempts = 0;
                    return dataResponse;
                }
                catch (WebException ex)
                {
                    var webResponse = (HttpWebResponse)ex.Response;
                    if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginAttempts += 1;
                        TryLogin();
                    }
                    else
                    {
                        return null;
                    }
 
                }
            }
            return null;
        }
 
        HttpWebRequest BuildRequest(string requestURIstring, string method = "GET", XElement entry = null)
        {
            var dataRequest = HttpWebRequest.Create(requestURIstring) as HttpWebRequest;
            dataRequest.Method = method;
            dataRequest.Accept = "application/atom+xml";
            dataRequest.ContentType = "application/atom+xml;type=entry";
 
            if (entry != null)
            {
                using (var writer = XmlWriter.Create(dataRequest.GetRequestStream()))
                {
                    entry.WriteTo(writer);
                }
            }
            return dataRequest;
        }
        void TryLogin()
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
                WriteCookiesToDisk();
            }
        }
        void WriteCookiesToDisk()
        {
            using (Stream stream = File.Create(CookiesFilePath))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, AuthCookie);
                }
                catch(Exception e)
                {
                    //ToDo: log error writing cookies to disk
                }
            }
        }
        void ReadCookiesFromDisk()
        {
            try
            {
                using (Stream stream = File.Open(CookiesFilePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    AuthCookie = (CookieContainer)formatter.Deserialize(stream);
                    CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                    CsrfToken = cookieCollection["BPMCSRF"].Value;
                }
            }
            catch(Exception e)
            {
                //ToDo: log error reading cookies from disk
            }
        }
    }
    class ODataFile
    {
        static void Main(string[] args)
        {
            GetOdataCollectionByAuthByHttpExample();
        }
        public static void GetOdataCollectionByAuthByHttpExample()
        {
            var helper = new ODataFileHelper();
            var uri = helper.serverUri + "ContactCollection?select=Id, Name";
            using (var dataResponse = helper.SendRequest(uri))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                }
            }
            using (var dataResponse = helper.SendRequest(uri))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                }
            }
            var content = new XElement(helper.dsmd + "properties",
                  new XElement(helper.ds + "Name", "Jhon Gilts"),
                  new XElement(helper.ds + "Dear", "Jhon"));
            var entry = new XElement(helper.atom + "entry",
                        new XElement(helper.atom + "content",
                        new XAttribute("type", "application/xml"), content));
            var uriForPost = helper.serverUri + "ContactCollection/";
            using (var dataResponse = helper.SendRequest(uriForPost, "POST", entry))
            {
                if (dataResponse != null)
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                }
            }
        }
    }
}