using CefSharp;
using CefSharp.OffScreen;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.AspNet.Core;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NewBurgerKingBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BurgerKingController : TwilioController
    {

        string fromNumber;
        string toNumber;
        string jsCode;
        IConfiguration configuration;

        public BurgerKingController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost]
        public void Index()
        {
            TwilioClient.Init(configuration["TwilioAccountSID"], configuration["TwilioAuthToken"]);
            fromNumber = Request.Form["From"];
            toNumber = Request.Form["To"];
            var burgerKingCode = Request.Form["Body"].ToString();
            if (burgerKingCode.Length !=int.Parse(configuration["Length"]))
            {
                MessageResource.Create(body: "kodunuz hatalı!!!", from: new(toNumber), to: new(fromNumber));
                return;
            }
            jsCode = configuration["jsCode"].Replace("BurgerKingCode", burgerKingCode);
            ChromiumWebBrowser chromiumWebBrowser = new(configuration["url"]);
            chromiumWebBrowser.FrameLoadEnd += WebBrowserFrameLoadEnded;
            
            MessageResource.Create(body: "kodunuz hazırlanmaya başladı",from: new(toNumber),to: new(fromNumber));
        }

        private void WebBrowserFrameLoadEnded(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                (sender as ChromiumWebBrowser).GetSourceAsync().ContinueWith(taskHtml =>
                {
                    var html = taskHtml.Result;
                    if (html.Contains("Kampanya Kodu"))
                    {
                        var kod = html.Substring(html.IndexOf("Kampanya Kodu"), 23);
                        MessageResource.Create(body: kod,from: new(toNumber),to: new(fromNumber));
                    }
                    else
                    {
                        (sender as ChromiumWebBrowser).ExecuteScriptAsync(jsCode);
                    }
                });
            }

        }
    }
}
