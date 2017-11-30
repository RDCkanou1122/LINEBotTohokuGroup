﻿using LineMessagingAPISDK;
using LineMessagingAPISDK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LINEBotApplication2.Controllers
{
    public class LineMessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post(HttpRequestMessage request)
        {
            if (!await VaridateSignature(request))
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            Activity activity = JsonConvert.DeserializeObject<Activity>
                (await request.Content.ReadAsStringAsync());

            // Line may send multiple events in one message, so need to handle them all.
            foreach (Event lineEvent in activity.Events)
            {
                LineMessageHandler handler = new LineMessageHandler(lineEvent);

                Profile profile = await handler.GetProfile(lineEvent.Source.UserId);

                switch (lineEvent.Type)
                {
                    case EventType.Beacon:
                        await handler.HandleBeaconEvent();
                        break;
                    case EventType.Follow:
                        await handler.HandleFollowEvent();
                        break;
                    case EventType.Join:
                        await handler.HandleJoinEvent();
                        break;
                    case EventType.Leave:
                        await handler.HandleLeaveEvent();
                        break;
                    case EventType.Message:
                        Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                await handler.HandleTextMessage();
                                break;
                            case MessageType.Audio:
                            case MessageType.Image:
                            case MessageType.Video:
                                await handler.HandleMediaMessage();
                                break;
                            case MessageType.Sticker:
                                await handler.HandleStickerMessage();
                                break;
                            case MessageType.Location:
                                await handler.HandleLocationMessage();
                                break;
                        }
                        break;
                    case EventType.Postback:
                        await handler.HandlePostbackEvent();
                        break;
                    case EventType.Unfollow:
                        await handler.HandleUnfollowEvent();
                        break;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private async Task<bool> VaridateSignature(HttpRequestMessage request)
        {
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ChannelSecret"].ToString()));
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(await request.Content.ReadAsStringAsync()));
            var contentHash = Convert.ToBase64String(computeHash);
            var headerHash = Request.Headers.GetValues("X-Line-Signature").First();

            return contentHash == headerHash;
        }
    }

    public class LineMessageHandler
    {
        private Event lineEvent;
        private LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());
        bool confirmFlg = true;
        public LineMessageHandler(Event lineEvent)
        {
            this.lineEvent = lineEvent;
        }

        public async Task HandleBeaconEvent()
        {
            await Reply(new TextMessage("HandleBeaconEvent"));
        }

        public async Task HandleFollowEvent()
        {
            await Reply(new TextMessage("HandleFollowEvent"));
        }

        public async Task HandleJoinEvent()
        {
            await Reply(new TextMessage("HandleJoinEvent"));
        }

        public async Task HandleLeaveEvent()
        {
            await Reply(new TextMessage("HandleLeaveEvent"));
        }

        public async Task HandlePostbackEvent()
        {
            string reply;
            // Handle DateTimePicker postback
            if (lineEvent.Postback?.Params != null)
            {
                var dateTime = lineEvent.Postback?.Params;
                reply = $"DateTime: {dateTime.DateTime}, Date: {dateTime.Date}, Time: {dateTime.Time}";
            }
            else
            {
                reply = lineEvent.Postback.Data;
            }
            await Reply(new TextMessage(reply));
        }

        public async Task HandleUnfollowEvent()
        {
            await Reply(new TextMessage("HandleUnfollowEvent"));
        }

        public async Task<Profile> GetProfile(string mid)
        {
            return await lineClient.GetProfile(mid);
        }

        public async Task HandleTextMessage()
        {
            var textMessage = JsonConvert.DeserializeObject<TextMessage>(lineEvent.Message.ToString());
            Message replyMessage = null;

            if (textMessage.Text.ToLower() == "ボタン")
            {
                List<TemplateAction> actions = new List<TemplateAction>();
                actions.Add(new MessageTemplateAction("Message Label", "sample data"));
                actions.Add(new PostbackTemplateAction("Postback Label", "sample data", "sample data"));
                actions.Add(new UriTemplateAction("Uri Label", "https://github.com/kenakamu"));
                ButtonsTemplate buttonsTemplate = new ButtonsTemplate("https://github.com/apple-touch-icon.png", "Sample Title", "Sample Text", actions);

                replyMessage = new TemplateMessage("Buttons", buttonsTemplate);
            }
            else if (textMessage.Text.ToLower() == "こんにちは")//confirm
            {
                List<TemplateAction> actions = new List<TemplateAction>();
                actions.Add(new MessageTemplateAction("はい", "yes"));
                actions.Add(new MessageTemplateAction("いいえ", "no"));
                ConfirmTemplate confirmTemplate = new ConfirmTemplate("こんにちは！テニスコートを予約しますか？", actions);
                replyMessage = new TemplateMessage("Confirm", confirmTemplate);

                //if (confirmTemplate.ToString() == "yes")
                //{
                //    confirmFlg = true;
                //    //replyMessage = new TextMessage("一覧を表示します♪");
                //}
                //else
                //{
                //    confirmFlg = false;
                //    //replyMessage = new TextMessage("しねーのかよ(# ﾟДﾟ)");
                //}
                
            }
            else if (textMessage.Text.ToLower() == "テニスコート予約したい" || textMessage.Text.ToLower() == "yes")
            {
                //replyMessage = new TextMessage("どこのコートを予約しますか？");//最初にメッセを出したかったが動作しない
                string aobayamaimg = @"~\Images\aobayama.PNG";//長命ヶ丘庭球場画像
                string tyoumeigaokaimg = @"~\Images\tyoumeigaoka.PNG";
                List<TemplateColumn> columns = new List<TemplateColumn>();
                List<TemplateAction> actions1 = new List<TemplateAction>();
                List<TemplateAction> actions2 = new List<TemplateAction>();

                actions1.Add(new UriTemplateAction("HP", "http://www.spf-sendai.jp/sisetsu/moshikomi.html"));
                actions1.Add(new UriTemplateAction("場所", "https://www.google.com/maps?ll=38.31376,140.83363&z=12&t=m&hl=ja&gl=US&mapclient=embed&cid=9042741886630709754"));
                actions1.Add(new UriTemplateAction("予約", "https://www.cm2.epss.jp/sendai/web/view/user/homeIndex.html"));
                columns.Add(new TemplateColumn() { Title = "長命ヶ丘庭球場", Text = "宮城県仙台市泉区長命ケ丘五丁目14", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions1 });

                actions2.Add(new UriTemplateAction("HP", "http://www.spf-sendai.jp/sisetsu/moshikomi.html"));
                actions2.Add(new UriTemplateAction("場所", "https://www.google.com/maps?ll=38.31376,140.83363&z=12&t=m&hl=ja&gl=US&mapclient=embed&cid=9042741886630709754"));
                actions2.Add(new UriTemplateAction("予約", "https://www.cm2.epss.jp/sendai/web/view/user/homeIndex.html"));
                columns.Add(new TemplateColumn() { Title = "青葉山庭球場", Text = "宮城県仙台市青葉区花壇1-1", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions2 });

                //// Add actions.
                //actions1.Add(new MessageTemplateAction("Message Label", "sample data"));
                //actions1.Add(new PostbackTemplateAction("Postback Label", "sample data", "sample data"));
                //actions1.Add(new UriTemplateAction("Uri Label", "https://github.com/kenakamu"));

                //// Add datetime picker actions
                //actions2.Add(new DatetimePickerTemplateAction("DateTime Picker", "DateTime", DatetimePickerMode.Datetime, "2017-07-21T13:00"));
                //actions2.Add(new DatetimePickerTemplateAction("Date Picker", "Date", DatetimePickerMode.Date, "2017-07-21"));
                //actions2.Add(new DatetimePickerTemplateAction("Time Picker", "Time", DatetimePickerMode.Time, "13:00"));

                //columns.Add(new TemplateColumn() { Title = "Casousel 1 Title", Text = "Casousel 1 Text", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions1 });
                //columns.Add(new TemplateColumn() { Title = "Casousel 2 Title", Text = "Casousel 2 Text", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions2 });

                CarouselTemplate carouselTemplate = new CarouselTemplate(columns);
                replyMessage = new TemplateMessage("Carousel", carouselTemplate);
            }
            else if (textMessage.Text.ToLower() == "イメージカルーセル")//imagecarousel
            {
                List<ImageColumn> columns = new List<ImageColumn>();
                UriTemplateAction action = new UriTemplateAction("Uri Label", "https://github.com/kenakamu");

                columns.Add(new ImageColumn("https://github.com/apple-touch-icon.png", action));
                columns.Add(new ImageColumn("https://github.com/apple-touch-icon.png", action));
                columns.Add(new ImageColumn("https://github.com/apple-touch-icon.png", action));
                columns.Add(new ImageColumn("https://github.com/apple-touch-icon.png", action));
                columns.Add(new ImageColumn("https://github.com/apple-touch-icon.png", action));

                ImageCarouselTemplate carouselTemplate = new ImageCarouselTemplate(columns);

                replyMessage = new TemplateMessage("Carousel", carouselTemplate);
            }
            else if (textMessage.Text.ToLower() == "イメージマップ")//imagemap
            {
                var url = HttpContext.Current.Request.Url;
                var imageUrl = $"{url.Scheme}://{url.Host}:{url.Port}/images/githubicon";
                List<ImageMapAction> actions = new List<ImageMapAction>();
                actions.Add(new UriImageMapAction("http://github.com", new ImageMapArea(0, 0, 520, 1040)));
                actions.Add(new MessageImageMapAction("I love LINE!", new ImageMapArea(520, 0, 520, 1040)));
                replyMessage = new ImageMapMessage(imageUrl, "GitHub", new BaseSize(1040, 1040), actions);
            }
            else if (textMessage.Text.ToLower() == "アッドリッチメニュー")//addrichmenu
            {
                // Create Rich Menu
                RichMenu richMenu = new RichMenu()
                {
                    Size = new RichMenuSize(1686),
                    Selected = false,
                    Name = "nice richmenu",
                    ChatBarText = "touch me",
                    Areas = new List<RichMenuArea>()
                        {
                            new RichMenuArea()
                            {
                                Action = new PostbackTemplateAction("action=buy&itemid=123"),
                                Bounds = new RichMenuBounds(0, 0, 2500, 1686)
                            }
                        }
                };


                var richMenuId = await lineClient.CreateRichMenu(richMenu);
                var image = new MemoryStream(File.ReadAllBytes(HttpContext.Current.Server.MapPath(@"~\Images\richmenu.PNG")));
                // Upload Image
                await lineClient.UploadRichMenuImage(richMenuId, image);
                // Link to user
                await lineClient.LinkRichMenuToUser(lineEvent.Source.UserId, richMenuId);
            }
            else if (textMessage.Text.ToLower() == "デリートリッチメニュー")//deleterichmenu
            {
                // Get Rich Menu for the user
                var richMenuId = await lineClient.GetRichMenuIdForUser(lineEvent.Source.UserId);

                await lineClient.UnlinkRichMenuToUser(lineEvent.Source.UserId);
                await lineClient.DeleteRichMenu(richMenuId);
            }
            else if (textMessage.Text.ToLower() == "デリートオールリッチメニュー")//deleteallrichmenu
            {
                // Get Rich Menu for the user
                var richMenuList = await lineClient.GetRichMenuList();
                foreach (var richMenu in richMenuList)
                {
                    await lineClient.DeleteRichMenu(richMenu["richMenuId"].ToString());
                }
            }
            else//どの条件にも合わない物はオウム返し
            {
                //replyMessage = new TextMessage(textMessage.Text);
            }
            await Reply(replyMessage);
        }

        public async Task HandleMediaMessage()
        {
            Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
            // Get media from Line server.
            Media media = await lineClient.GetContent(message.Id);
            Message replyMessage = null;

            // Reply Image 
            switch (message.Type)
            {
                case MessageType.Image:
                case MessageType.Video:
                case MessageType.Audio:
                    replyMessage = new ImageMessage("https://github.com/apple-touch-icon.png", "https://github.com/apple-touch-icon.png");
                    break;
            }

            await Reply(replyMessage);
        }

        public async Task HandleStickerMessage()
        {
            //https://devdocs.line.me/files/sticker_list.pdf
            var stickerMessage = JsonConvert.DeserializeObject<StickerMessage>(lineEvent.Message.ToString());
            var replyMessage = new StickerMessage("1", "1");
            await Reply(replyMessage);
        }

        public async Task HandleLocationMessage()
        {
            var locationMessage = JsonConvert.DeserializeObject<LocationMessage>(lineEvent.Message.ToString());
            LocationMessage replyMessage = new LocationMessage(
                locationMessage.Title,
                locationMessage.Address,
                locationMessage.Latitude,
                locationMessage.Longitude);
            await Reply(replyMessage);
        }

        private async Task Reply(Message replyMessage)
        {
            try
            {
                await lineClient.ReplyToActivityAsync(lineEvent.CreateReply(message: replyMessage));
            }
            catch
            {
                await lineClient.PushAsync(lineEvent.CreatePush(message: replyMessage));
            }
        }
    }
}
