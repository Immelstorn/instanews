using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using InstagramNews.Models.Instagram;
using Newtonsoft.Json;

namespace InstagramNews.Controllers
{
    public class HomeController : Controller
    {
#if DEBUG
        private const string ClientID = ""; //local
        private const string ClientSecret = ""; //local
        private const string RedirectUrl = ""; //local
#else   
        private const string ClientID = "";
        private const string ClientSecret = "";
        private const string RedirectUrl = "http://instanews.apphb.com/Home/GetCode";
#endif

        private const string NewsUri = @"http://instagram.com/api/v1/news/";
        private const string LoginUri = @"https://instagram.com/accounts/login/";

        private const string SecondLoginUri =
                "https://instagram.com/accounts/login/ajax/?targetOrigin=https%3A%2F%2Finstagram.com&showFacebookLogin=true&__a=1";

        private const string RefererUri =
                "https://instagram.com/accounts/login/ajax/facebook/?targetOrigin=https%3A%2F%2Finstagram.com";

        private string accessCode;

        private string AccessToken
        {
            get
            {
                HttpCookie cookie2 = Request.Cookies ["AccessToken"];
                return cookie2 != null ? cookie2.Value : string.Empty;
            }
            set { Response.Cookies.Set(new HttpCookie("AccessToken", value)); }
        }

        private string SessionIdCookie
        {
            get
            {
                HttpCookie cookie2 = Request.Cookies ["sessionIdCookie"];
                return cookie2 != null ? cookie2.Value : string.Empty;
            }
            set { Response.Cookies.Set(new HttpCookie("sessionIdCookie", value)); }
        }

        public async Task<ActionResult> Index()
        {
            if(string.IsNullOrEmpty(SessionIdCookie))
            {
                return View(new News());
            }

            if(string.IsNullOrEmpty(AccessToken))
            {
                return Authorize();
            }

            string res = await GetNews();
            if(res == null)
            {
                return View(new News
                {
                    ValidationError = "You must provide valid Username with Password"
                });
            }
            var news = new News {SessionIdSet = true};
            return View(news);
        }

        public async Task<string> GetNews()
        {
            string result = string.Empty;
            try
            {
                result = await HttpRequest(NewsUri);
            }
            catch(WebException e)
            {
                if(e.Message.Contains("(400) Bad Request"))
                {
                    return null;
                }
            }
            return result;
        }

        public async Task<PartialViewResult> GetNewsPartial(int timestamp)
        {
            string res = await GetNews();
            if(res == null)
            {
                return PartialView("NewsPartial", new News());
            }
            var news = JsonConvert.DeserializeObject<News>(res);
            List<Story> result = news.stories.Where(s => s.args.timestamp > timestamp).ToList();
            news.stories = result;
            return PartialView("NewsPartial", news);
        }

        public async Task<int> GetNewsAfterCount(int timestamp)
        {
            string result = string.Empty;
            try
            {
                result = await HttpRequest(NewsUri);
            }
            catch(WebException e)
            {
                if(e.Message.Contains("(400) Bad Request"))
                {
                    return 0;
                }
            }
            return JsonConvert.DeserializeObject<News>(result).stories.Count(s => s.args.timestamp > timestamp);
        }

        public async Task<JsonResult> GetPhotoData(string id)
        {
            if(string.IsNullOrEmpty(AccessToken))
            {
                return Json(new {error = true}, JsonRequestBehavior.AllowGet);
            }

            string jsonMedia = await
                    HttpRequest(string.Format("https://api.instagram.com/v1/media/{0}?access_token={1}",
                            id,
                            AccessToken));
            var mediaObject = JsonConvert.DeserializeObject<RootObject<Media>>(jsonMedia);
            return Json(new
            {
                error = false,
                url = mediaObject.data.link,
                liked = mediaObject.data.user_has_liked,
                big = mediaObject.data.images.standard_resolution.url,
                medium = mediaObject.data.images.low_resolution.url,
                low = mediaObject.data.images.thumbnail.url,
                caption = mediaObject.data.caption == null ? string.Empty : mediaObject.data.caption.text,
                mediaObject.data.user.username,
                likes = mediaObject.data.likes.count,
                comments = mediaObject.data.comments.count,
            },
                    JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> Like(string id, bool liked)
        {
            if(string.IsNullOrEmpty(AccessToken))
            {
                return Json(new {error = true}, JsonRequestBehavior.AllowGet);
            }

            await HttpRequest(string.Format("https://api.instagram.com/v1/media/{0}/likes?access_token={1}",
                    id,
                    AccessToken),
                    liked ? "DELETE" : "POST");
            return Json(new
            {
                error = false,
            },
                    JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetUserImage(string id)
        {
            if(string.IsNullOrEmpty(AccessToken))
            {
                return Json(new {error = true}, JsonRequestBehavior.AllowGet);
            }

            string jsonUser = await
                    HttpRequest(string.Format("https://api.instagram.com/v1/users/{0}?access_token={1}",
                            id,
                            AccessToken));
            var userObject = JsonConvert.DeserializeObject<RootObject<User>>(jsonUser);
            return Json(new
            {
                error = false,
                image = userObject.data.profile_picture
            },
                    JsonRequestBehavior.AllowGet);
        }

        public async Task<RedirectResult> GetCode(string code)
        {
            accessCode = code;
            var postData = new Dictionary<string, string>
            {
                {"client_id", ClientID},
                {"client_secret", ClientSecret},
                {"grant_type", "authorization_code"},
                {"redirect_uri", RedirectUrl},
                {"code", accessCode}
            };

            string json = await HttpRequest(@"https://api.instagram.com/oauth/access_token", "POST", postData);
            AccessToken = JsonConvert.DeserializeObject<AutorizationModel>(json).access_token;
            return Redirect("/");
        }

        private RedirectResult Authorize()
        {
            string uri =
                    string.Format(
                                  @"https://api.instagram.com/oauth/authorize/?client_id={0}&redirect_uri={1}&response_type=code&scope=basic+likes",
                            ClientID,
                            RedirectUrl);
            return Redirect(uri);
        }

        private async Task<string> HttpRequest(string uri,
                string method = "GET",
                Dictionary<string, string> requestData = null)
        {
            byte[] data = null;
            if(requestData != null)
            {
                string postData = requestData.Aggregate(string.Empty,
                        (current, pair) => current + (pair.Key + "=" + pair.Value + "&"));
                postData = postData.Substring(0, postData.Length - 1);
                var encoding = new ASCIIEncoding();
                data = encoding.GetBytes(postData);
            }

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            if(request != null)
            {
                request.Proxy = null;
                request.UserAgent =
                        "Instagram 6.3.1 Android (19/4.4.4; 320dpi; 768x1184; LGE/google; Nexus 4; mako; mako; ru_RU)";
                var cc = new CookieContainer();
                cc.Add(new Uri(NewsUri), new Cookie("sessionid", SessionIdCookie));
                request.CookieContainer = cc;
                request.Method = method;

                if(data != null)
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = data.Length;
                    using(Stream newStream = await request.GetRequestStreamAsync())
                    {
                        newStream.Write(data, 0, data.Length);
                    }
                }
            }

            string stream = "";
            if(request != null)
            {
                using(var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    if(response != null)
                    {
                        using(Stream receiveStream = response.GetResponseStream())
                        {
                            if(receiveStream != null)
                            {
                                using(var readStream = new StreamReader(receiveStream, Encoding.UTF8))
                                {
                                    stream = readStream.ReadToEnd();
                                }
                            }
                        }
                        Cookie cookie = response.Cookies ["sessionid"];
                        if(cookie != null && cookie.Value != SessionIdCookie)
                        {
                            SessionIdCookie = cookie.Value;
                        }
                    }
                }
            }
            return stream;
        }

        public async Task<ActionResult> Login(string username, string password)
        {
            var model = new News();
            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                CookieContainer cc = await ProceedLogin(username, password);
                Cookie cookie = cc.GetCookies(new Uri(NewsUri)) ["sessionid"];
                if(cookie != null && !string.IsNullOrEmpty(cookie.Value))
                {
                    SessionIdCookie = cookie.Value;
                    return Redirect("/");
                }
                model.ValidationError = "You must provide VALID Username with password";
                return View("Index", model);
            }
            model.ValidationError = "You must provide Username with password";
            return View("Index", model);
        }

        private async Task<CookieContainer> ProceedLogin(string username, string password)
        {
            var cc = new CookieContainer();
            var url = new Uri(LoginUri);
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.CookieContainer = cc;

            WebResponse response = await request.GetResponseAsync();

            string parameters = string.Format("csrfmiddlewaretoken={0}&username={1}&password={2}&next=",
                    cc.GetCookies(url) ["csrftoken"].Value,
                    username,
                    password);

            request = (HttpWebRequest) WebRequest.Create(SecondLoginUri);
            request.Host = "instagram.com";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("DNT", "1");
            request.Referer = RefererUri;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";
            request.CookieContainer = cc;
            request.Headers.Add("Cookie", response.Headers.Get("Set-Cookie"));

            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            request.ContentLength = byteArray.Length;

            using(Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            response = await request.GetResponseAsync();
            response.Close();
            return cc;
        }
    }
}