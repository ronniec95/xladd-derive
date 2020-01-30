using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace AARC.Utilities
{
    // To get data via a proxy server instead of directly... TODO: should it not be a utility rather than a datalayer object?
        public class AarcProxy
        {
            // This method doesn't use the proxy.
            public static string GetHtml(string url)
            {
                string data = null;
                // Get data from the web... 
                try
                {
                    using (WebClient web = new WebClient())
                    {
                        data = web.DownloadString(url);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception AarcProxy.GetHtml(\"{0}\"):\r\n  {1}", url, ex.Message);
                    if (ex.InnerException != null)
                        Console.WriteLine("  {0}", ex.InnerException.Message);
                }
                return data;
            }

            public static string[] LoadUrl(string url)
            {
                string data = GetHtml(url);
                data = data?.Replace("\r", "");
                return data?.Split('\n');
            }


            // Russia: 213.85.92.10:80 HTTP
            // Netherlands: 88.159.96.236:80 HTTP
            // Brazil: 201.72.254.82:80 HTTPS

            // "http://177.128.193.109:8089" Brazil
            //private static string _proxy = "195.116.53.251:3128";   // poland
            //private static string _proxy = "54.195.48.153:8888";  // USA
            // 178.22.148.122	3129	 flag France

            public static void TestGet()
            {
                string s = GetUrl("https://www.atagar.com/echo.php");
                Console.WriteLine(s);
            }

            private static List<string> ProxyList()
            {
                return new List<string> { "54.195.48.153:8888", "178.33.191.53:3128", "201.150.148.82:8080" };//,  "178.22.148.122:3129", "213.85.92.10:80" };
            }

            private static WebProxy _currentProxy;

            public static string GetUrl(string url)
            {
                // try each proxy in ProxyList()
                string responseString = null;

                // split obtaining the proxy, and sending the request.. ?
                // continue using a proxy, until it fails..

                List<string> proxies = ProxyList();

                for (int i = 0; i < proxies.Count; i++)
                {
                    // use the current proxy if it worked last time, or try to get another proxy if required
                    if (_currentProxy != null)
                    {
                        i = -1;     // gives the chance to use all proxies, if this one fails
                    }
                    else
                    {
                        try
                        {
                            Console.Write(" Trying to get proxy to: {0}    ", proxies[i]);
                            _currentProxy = new WebProxy(proxies[i], false);
                            Console.WriteLine(" succeeded");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        if (_currentProxy == null)
                            continue;
                    }

                    try
                    {
                        Console.WriteLine(" Proxy={0} Trying to load url: {1}", _currentProxy.Address, url);
                        WebRequest request = WebRequest.Create(url);
                        request.Proxy = _currentProxy;
                        request.Method = "GET";

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        //var getHtmlWeb = new HtmlWeb() { AutoDetectEncoding = false, OverrideEncoding = Encoding.GetEncoding("iso-8859-2") };
                        //WebProxy myproxy = new WebProxy("127.0.0.1:8888", false);
                        //NetworkCredential cred = (NetworkCredential)CredentialCache.DefaultCredentials;
                        //var document = getHtmlWeb.Load("URL", "GET", myproxy, cred);

                        Stream responseStream = response.GetResponseStream();
                        if (responseStream != null)// && responseStream != Stream.Null)
                        {
                            Encoding enc = Encoding.GetEncoding(1252);
                            StreamReader loResponseStream = new StreamReader(responseStream, enc);

                            responseString = loResponseStream.ReadToEnd();

                            loResponseStream.Close();
                        }

                        response.Close();

                        break;  // break on successfully obtaining the html using one of the proxies
                    }
                    catch (WebException webex)
                    {
                        Console.WriteLine(webex.Message);
                        if (webex.InnerException != null)
                            Console.WriteLine(webex.InnerException.Message);

                        HttpWebResponse wr = webex.Response as HttpWebResponse;
                        if (wr != null && wr.StatusCode == HttpStatusCode.NotFound)
                        {
                            Console.WriteLine("This file was not found on the server, so marking it as not found by saving Expired.html");
                            responseString = File.ReadAllText("E:\\Dev\\AARC\\Earnings\\Expired.html");
                            break;
                        }
                        else
                        {
                            // try another proxy...
                            _currentProxy = null;
                        }

                        // and (503) Server Unavailable ?? wait and try later?

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);

                        // this proxy did not work, so try another...
                        _currentProxy = null;
                    }
                }

                return responseString;
            }
        }
    }

