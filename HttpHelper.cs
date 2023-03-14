using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Threading;

using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace MyWordNotify
{
    public class HttpHelper
    {
        const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";

        Encoding pageEncoding = Encoding.UTF8;

        CookieContainer cookie;
        WebProxy proxy;
        Dictionary<string, string> headers = null;

        string _contenttype = "application/x-www-form-urlencoded";

        public HttpHelper(Encoding encoding)
        {
            pageEncoding = encoding;
            cookie = new CookieContainer();
            proxy = new WebProxy();
        }

        private bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; 
        }

        public string ContentType
        {
            get { return _contenttype; }
            set { _contenttype = value; }
        }

        public void SetHeader(Dictionary<string, string> Headers)
        {
            headers = Headers;
        }

        public string UrlGet(string Url, ref int HttpCode, int TryCount)
        {
            return HttpGet(Url, ref HttpCode, TryCount, "");
        }

        public string UrlGet(string Url, ref int HttpCode)
        {
            return HttpGet(Url, ref HttpCode, 3, "");
        }

        public string UrlGet(string Url, ref int HttpCode, string Referer)
        {
            return HttpGet(Url, ref HttpCode, 3, Referer);
        }

        public string UrlGet(string Url, ref int HttpCode, int TryCount, string Referer)
        {
            return HttpGet(Url, ref HttpCode, TryCount, Referer);
        }

        public void UrlDownText(string Url, string FilePath)
        {
            int code = 0;
            string ct = HttpGet(Url, ref code, 0, "");
            if (code == 200)
            {
                if (!string.IsNullOrEmpty(ct))
                {
                    File.WriteAllText(FilePath, ct);
                }
            }
        }

        /// <summary>
        /// 清除Cookies
        /// </summary>
        public void ClearCookies()
        {
            if (cookie != null)
            {
                cookie = new CookieContainer();
            }
        }


        

        /// <summary>
        /// 获取当前代理地址和端口
        /// </summary>
        /// <returns></returns>
        public string GetProxyIpPort()
        {
            if (proxy != null && proxy.Address != null)
            {
                return string.Format("{0}:{1}", proxy.Address.Host, proxy.Address.Port);
            }

            return "";
        }



        public void UrlDownFile(string Url, string FilePath, string Referer = "")
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                req.Method = "GET";
                req.UserAgent = USERAGENT;
                req.Timeout = 60 * 60 * 1000; //60分钟
                req.KeepAlive = true;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                if (!string.IsNullOrEmpty(Referer))
                {
                    req.Referer = Referer;
                }
                SetRequestHeader(req);

                if (Url.StartsWith("https://"))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.DefaultConnectionLimit = 100;
                    ServicePointManager.Expect100Continue = false;
                }


                req.CookieContainer = cookie;
                if (proxy.Address != null)
                {
                    req.Proxy = proxy;
                }

                int HttpCode = 0;

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                if (res != null)
                {
                    HttpCode = (int)res.StatusCode;



                    Stream s = res.GetResponseStream();
                    using (FileStream fs = File.Create(FilePath))
                    {
                        byte[] btfile = new byte[1024];
                        int n = 1;
                        while (n > 0)
                        {
                            n = s.Read(btfile, 0, 1024);
                            fs.Write(btfile, 0, n);
                        }

                    }
                    s.Close();
                }
                res.Close();
            }
            catch
            {
            }
        }

        protected string HttpGet(string Url, ref int HttpCode, int TryCount, string Referer)
        {
            string ret = string.Empty;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                req.Method = "GET";
                req.UserAgent = USERAGENT;
                req.Timeout = 600 * 1000; //10分钟
                req.KeepAlive = true;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                SetRequestHeader(req);

                if (Url.StartsWith("https://"))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.DefaultConnectionLimit = 100;
                    ServicePointManager.Expect100Continue = false;
                }


                if (!string.IsNullOrEmpty(Referer))
                {
                    req.Referer = Referer;
                }

                req.CookieContainer = cookie;
                if (proxy.Address != null)
                {
                    req.Proxy = proxy;
                }

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                if (res != null)
                {
                    HttpCode = (int)res.StatusCode;
                    Stream s = res.GetResponseStream();
                    StreamReader sr = new StreamReader(s, pageEncoding);
                    ret = sr.ReadToEnd();
                    sr.Close();
                    s.Close();
                }
                res.Close();
            }
            catch (WebException ex)
            {
                if (TryCount == 0)
                {
                    HttpCode = (int)ex.Status;
                    ret = ex.Message;
                }
                else
                {
                    if (TryCount == 1)
                    {
                        Thread.Sleep(60 * 1000); //1分钟
                    }
                    else
                    {
                        Thread.Sleep(20000); //20秒
                    }
                    TryCount--;
                    ret = HttpGet(Url, ref HttpCode, TryCount, Referer);
                }
            }


            return ret;
        }

        public string UrlPost(string Url, string Data, ref int HttpCode)
        {
            string ret = string.Empty;

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                req.Method = "POST";
                req.UserAgent = USERAGENT;
                req.Timeout = 30 * 60 * 1000; //30分钟

                SetRequestHeader(req);

                if (Url.StartsWith("https://"))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.DefaultConnectionLimit = 100;
                    ServicePointManager.Expect100Continue = false;
                }



                byte[] btData = Encoding.UTF8.GetBytes(Data);
                req.ContentLength = btData.Length;
                req.ContentType = ContentType;

                req.CookieContainer = cookie;
                if (proxy.Address != null)
                {
                    req.Proxy = proxy;
                }


                Stream sreq = req.GetRequestStream();
                sreq.Write(btData, 0, btData.Length);
                sreq.Close();

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                if (res != null)
                {
                    HttpCode = (int)res.StatusCode;
                    Stream s = res.GetResponseStream();
                    StreamReader sr = new StreamReader(s, pageEncoding);
                    ret = sr.ReadToEnd();
                    sr.Close();
                    s.Close();
                }
                res.Close();
            }
            catch (WebException ex)
            {
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    HttpCode = (int)httpResponse.StatusCode;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        ret = reader.ReadToEnd();
                    }
                }
            }
            finally
            {
            }

            return ret;
        }

        /// <summary>
        /// 设置Http头
        /// </summary>
        /// <param name="Req"></param>
        protected void SetRequestHeader(HttpWebRequest Req)
        {
            if (headers != null && headers.Count != 0)
            {
                foreach (var h in headers)
                {
                    Req.Headers.Add(h.Key, h.Value);
                }
            }
        }
    }
}
