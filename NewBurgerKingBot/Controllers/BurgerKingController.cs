using System;
using CefSharp;
using CefSharp.OffScreen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

        private string _fromNumber;
        private string _toNumber;
        private string _jsCode;
        private readonly IConfiguration _configuration;

        public BurgerKingController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpPost]
        public void Index()
        {
            TwilioClient.Init(_configuration["TwilioAccountSID"], _configuration["TwilioAuthToken"]);
            _fromNumber = Request.Form["From"];
            _toNumber = Request.Form["To"];
            var burgerKingCode = Request.Form["Body"].ToString();
            if (burgerKingCode.Length != int.Parse(_configuration["Length"]))
            {
                MessageResource.Create(body: "kodunuz hatalı!!!", from: new PhoneNumber(_toNumber), to: new PhoneNumber(_fromNumber));
                return;
            }
            _jsCode = _configuration["jsCode"].Replace("BurgerKingCode", burgerKingCode);
            ChromiumWebBrowser chromiumWebBrowser = new(_configuration["url"]);
            chromiumWebBrowser.FrameLoadEnd += WebBrowserFrameLoadEnded;

            MessageResource.Create(body: "kodunuz hazırlanmaya başladı", from: new PhoneNumber(_toNumber), to: new PhoneNumber(_fromNumber));
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
                        var kod = html.Substring(html.IndexOf("Kampanya Kodu", StringComparison.Ordinal), 23);
                        MessageResource.Create(body: kod, from: new PhoneNumber(_toNumber), to: new PhoneNumber(_fromNumber));
                    }
                    else
                    {
                        (sender as ChromiumWebBrowser).ExecuteScriptAsync(_jsCode);
                    }
                });
            }

        }
    }
}
