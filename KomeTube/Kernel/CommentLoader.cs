using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using KomeTube.Kernel.YtLiveChatDataModel;

namespace KomeTube.Kernel
{
    public class CommentLoader
    {
        #region Private Member

        private String _videoUrl;
        private CookieContainer _cookieContainer;
        private InnerTubeContextData _innerContextData;
        private readonly Object _lockContinuation;

        private Task _mainTask;
        private CancellationTokenSource _mainTaskCancelTS;

        #endregion Private Member

        #region Constructor

        public CommentLoader()
        {
            _videoUrl = "";
            _lockContinuation = new object();
        }

        #endregion Constructor

        #region Public Member

        /// <summary>
        /// 使用者輸入的直播影片網址
        /// </summary>
        public String VideoUrl
        {
            get
            {
                return _videoUrl;
            }

            set
            {
                _videoUrl = value;
            }
        }

        /// <summary>
        /// 當前的continuation資料，用來取得下次留言列表
        /// </summary>
        public String CurrentContinuation
        {
            get
            {
                lock (_lockContinuation)
                {
                    return _innerContextData.continuation;
                }
            }

            set
            {
                lock (_lockContinuation)
                {
                    _innerContextData.continuation = value;
                }
            }
        }

        /// <summary>
        /// CommentLoader當前執行狀態
        /// </summary>
        public CommentLoaderStatus Status { get; private set; }

        #endregion Public Member

        #region Public Method

        /// <summary>
        /// 開始讀取留言
        /// <para>請監聽OnCommentsReceive事件取得留言列表</para>
        /// </summary>
        /// <param name="url">Youtube直播影片位址</param>
        public void Start(String url)
        {
            if (_mainTask != null
                && !_mainTask.IsCompleted)
            {
                //若任務仍在執行則不再重複啟動
                return;
            }

            _mainTaskCancelTS = new CancellationTokenSource();
            _mainTask = Task.Factory.StartNew(() => StartGetComments(url), _mainTaskCancelTS.Token);
            _mainTask.ContinueWith(StartGetCommentsCompleted, _mainTaskCancelTS.Token);
        }

        /// <summary>
        /// 停止取得留言
        /// </summary>
        public void Stop()
        {
            if (_mainTaskCancelTS != null)
            {
                //停止取得留言
                _mainTaskCancelTS.Cancel();
                RaiseStatusChanged(CommentLoaderStatus.StopRequested);
            }
        }

        #endregion Public Method

        #region Private Method

        /// <summary>
        /// 從影片位址解析vid後取得聊天室位址
        /// </summary>
        /// <param name="videoUrl">影片位址</param>
        /// <returns>回傳聊天室位址，若失敗則發出CanNotGetLiveChatUrl Error事件，並回傳空字串</returns>
        private String GetLiveChatRoomUrl(String videoUrl)
        {
            const String baseUrl = "www.youtube.com/watch?";
            String ret = "";
            String urlParamStr = videoUrl.Substring(videoUrl.IndexOf(baseUrl) + baseUrl.Length);
            String[] urlParamArr = urlParamStr.Split('&');
            String vid = "";

            //取得vid
            foreach (String param in urlParamArr)
            {
                if (param.IndexOf("v=") == 0)
                {
                    vid = param.Substring(2);
                    break;
                }
            }

            if (vid == "")
            {
                Debug.WriteLine(String.Format("[GetLiveChatRoomUrl] 無法取得聊天室位址. URL={0}", videoUrl));
                RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatUrl, videoUrl);
                return "";
            }
            else
            {
                ret = String.Format("https://www.youtube.com/live_chat?v={0}&is_popout=1", vid);
            }

            return ret;
        }

        /// <summary>
        /// 取得Youtube API 'get_live_chat'的位址
        /// </summary>
        /// <param name="apiKey">YtCfg資料中INNERTUBE_API_KEY參數。此參數應從ParseLiveChatHtml或GetComment方法取得</param>
        /// <returns>回傳Youtube API 'get_live_chat'的位址</returns>
        private String GetLiveChatUrl(String apiKey)
        {
            string ret = @"https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?key=" + apiKey;

            return ret;
        }

        /// <summary>
        /// 開始取得留言，此方法將會進入長時間迴圈，若要停止請使用_mainTaskCancelTS發出cancel請求
        /// </summary>
        /// <param name="url">直播影片位址</param>
        private void StartGetComments(String url)
        {
            RaiseStatusChanged(CommentLoaderStatus.Started);

            String continuation = "";

            //取得聊天室位址
            String liveChatRoomUrl = GetLiveChatRoomUrl(url);
            if (liveChatRoomUrl == "")
            {
                Debug.WriteLine(String.Format("[StartGetComments] GetLiveChatRoomUrl無法取得html內容"));
                return;
            }

            //取得continuation和第一次訪問的留言列表
            List<CommentData> firstCommentList = ParseLiveChatHtml(liveChatRoomUrl, ref continuation);
            if (continuation == "")
            {
                Debug.WriteLine(String.Format("[StartGetComments] ParseLiveChatHtml無法取得continuation參數"));
                return;
            }

            this.VideoUrl = url;
            RaiseCommentsReceive(firstCommentList);

            //持續取得留言
            while (!_mainTaskCancelTS.IsCancellationRequested)
            {
                List<CommentData> comments = GetComments(ref continuation);

                if (comments != null
                    && comments.Count > 0)
                {
                    RaiseCommentsReceive(comments);
                }

                if (continuation == "")
                {
                    Debug.WriteLine(String.Format("[StartGetComments] GetComments無法取得continuation參數"));
                    return;
                }

                SpinWait.SpinUntil(() => false, 1000);
            }
        }

        /// <summary>
        /// 取得留言的Task結束(StartGetComments方法結束)
        /// </summary>
        /// <param name="sender">已完成的Task</param>
        /// <param name="obj"></param>
        private void StartGetCommentsCompleted(Task sender, object obj)
        {
            if (sender.IsFaulted)
            {
                //取得留言時發生其他exception造成Task結束
                RaiseError(CommentLoaderErrorCode.GetCommentsError, sender.Exception.Message);
            }

            RaiseStatusChanged(CommentLoaderStatus.Completed);
        }

        /// <summary>
        /// 取得第一次訪問的cookie資料，並在html中取出ytcfg資料與window["ytInitialData"] 後方的json code，並解析出continuation
        /// </summary>
        /// <param name="liveChatUrl">聊天室位址</param>
        /// <returns>回傳continuation參數值</returns>
        private List<CommentData> ParseLiveChatHtml(String liveChatUrl, ref String continuation)
        {
            String htmlContent = "";
            List<CommentData> initComments = new List<CommentData>();

            RaiseStatusChanged(CommentLoaderStatus.GetLiveChatHtml);
            CookieContainer cc = new CookieContainer();

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cc,
            };

            //取得HTML內容
            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0");
                    htmlContent = client.GetStringAsync(liveChatUrl).Result;
                    _cookieContainer = handler.CookieContainer;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法取得聊天室HTML內容. Excetion:{0}", e.Message));
                    RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatHtml, e.Message);
                    return null;
                }
            }

            //解析YtCfg
            RaiseStatusChanged(CommentLoaderStatus.ParseYtCfgData);
            string strCfg = ParseYtCfg(htmlContent);
            if (strCfg == null)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析YtCfg. HTML content:{0}", htmlContent));
                RaiseError(CommentLoaderErrorCode.CanNotParseYtCfg, htmlContent);
                return null;
            }
            //解析inner context data
            _innerContextData = ParseInnerContextData(strCfg);

            //解析HTML
            RaiseStatusChanged(CommentLoaderStatus.ParseLiveChatHtml);
            Match match = Regex.Match(htmlContent, "window\\[\"ytInitialData\"\\] = ({.+});\\s*</script>", RegexOptions.Singleline);
            if (!match.Success)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析HTML. HTML content:{0}", htmlContent));
                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, htmlContent);
                return null;
            }

            //解析json data
            String ytInitialData = match.Groups[1].Value;
            dynamic jsonData;
            try
            {
                jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(ytInitialData);

                var data = jsonData["contents"]["liveChatRenderer"]["continuations"][0]["invalidationContinuationData"];
                if (data == null)
                {
                    data = jsonData["contents"]["liveChatRenderer"]["continuations"][0]["timedContinuationData"];
                }
                continuation = Convert.ToString(JsonHelper.TryGetValue(data, "continuation", ""));
                _innerContextData.continuation = continuation;

                var actions = jsonData["contents"]["liveChatRenderer"]["actions"];
                initComments = ParseComment(actions);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析json data:{0}", e.Message));
                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, ytInitialData);
                return null;
            }

            return initComments;
        }

        /// <summary>
        /// 從第一次訪問的html內容解析出ytcfg字串
        /// </summary>
        /// <param name="liveChatHtml"></param>
        /// <returns></returns>
        private string ParseYtCfg(string liveChatHtml)
        {
            var match = Regex.Match(liveChatHtml, "ytcfg\\.set\\(({.+?})\\);", RegexOptions.Singleline);
            if (!match.Success)
            {
                return null;
            }

            try
            {
                var ytCfg = match.Groups[1].Value;
                dynamic d = JsonConvert.DeserializeObject(ytCfg);
                var matches = Regex.Matches(liveChatHtml, "ytcfg\\.set\\(\"([^\"]+)\",\\s*(.+?)\\);?\\r?\n", RegexOptions.Singleline);
                foreach (Match m in matches)
                {
                    var key = m.Groups[1].Value;
                    var value = m.Groups[2].Value;
                    var s = "{\"" + key + "\":" + value + "}";
                    var obb = JsonConvert.DeserializeObject(s);
                    d.Merge(obb);
                }
                return d.ToString(Formatting.None);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format($"[ParseYtCfg] 無法解析YtCfg資料:{e.Message}"));
                return null;
            }
        }

        /// <summary>
        /// 從YtCfg字串解析inner context資料供取得留言使用
        /// </summary>
        /// <param name="strCfg"></param>
        /// <returns></returns>
        private InnerTubeContextData ParseInnerContextData(string strCfg)
        {
            InnerTubeContextData ret = new InnerTubeContextData();
            dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(strCfg);

            ret.INNERTUBE_API_KEY = Convert.ToString(JsonHelper.TryGetValue(jsonData, "INNERTUBE_API_KEY", ""));

            ret.context.clickTracking.clickTrackingParams = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.clickTracking.clickTrackingParams", ""));

            ret.context.request.useSsl = Convert.ToBoolean(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.request.useSsl", false));

            ret.context.client.browserName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserName", ""));
            ret.context.client.browserVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserVersion", ""));
            ret.context.client.clientFormFactor = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientFormFactor", ""));
            ret.context.client.clientName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientName", ""));
            ret.context.client.clientVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientVersion", ""));
            ret.context.client.deviceMake = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceMake", ""));
            ret.context.client.deviceModel = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceModel", ""));
            ret.context.client.gl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.gl", ""));
            ret.context.client.hl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.hl", ""));
            ret.context.client.originalUrl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.originalUrl", ""));
            ret.context.client.osName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osName", ""));
            ret.context.client.osVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osVersion", ""));
            ret.context.client.platform = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.platform", ""));
            ret.context.client.remoteHost = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.remoteHost", ""));
            ret.context.client.userAgent = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.userAgent", ""));
            ret.context.client.visitorData = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.visitorData", ""));

            return ret;
        }

        /// <summary>
        /// 利用Youtube API 'get_live_chat'取得聊天室留言，並解析continuation參數供下次取得留言使用
        /// </summary>
        /// <param name="continuation">Continuation參數</param>
        /// <returns>成功時回傳留言資料，失敗則回傳null。</returns>
        private List<CommentData> GetComments(ref String continuation)
        {
            if (continuation == null || continuation == "")
            {
                RaiseError(CommentLoaderErrorCode.GetCommentsError, new Exception("continuation參數錯誤"));
                return null;
            }

            RaiseStatusChanged(CommentLoaderStatus.GetComments);

            String chatUrl = GetLiveChatUrl(_innerContextData.INNERTUBE_API_KEY);
            List<CommentData> ret = null;
            String resp = "";
            HttpClientHandler handler = new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = _cookieContainer,
            };

            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    StringContent dataContent = new StringContent(_innerContextData.ToString(), Encoding.UTF8, "application/json");

                    //取得聊天室留言
                    client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.youtube.com");

                    HttpResponseMessage respMsg = client.PostAsync(chatUrl, dataContent).Result;
                    resp = respMsg.Content.ReadAsStringAsync().Result;

                    //解析continuation供下次取得留言使用
                    dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(resp);
                    var data = jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["invalidationContinuationData"];
                    if (data == null)
                    {
                        data = jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["timedContinuationData"];
                    }
                    continuation = Convert.ToString(JsonHelper.TryGetValue(data, "continuation", ""));
                    _innerContextData.continuation = continuation;
                    _cookieContainer = handler.CookieContainer;

                    //解析留言資料
                    var commentActions = jsonData["continuationContents"]["liveChatContinuation"]["actions"];
                    ret = ParseComment(commentActions);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("[GetComments] 無法取得聊天室HTML內容. Excetion:{0}", e.Message));
                    RaiseError(CommentLoaderErrorCode.GetCommentsError, e.Message);
                    return null;
                }
            }

            return ret;
        }

        /// <summary>
        /// 解析留言資訊
        /// </summary>
        /// <param name="commentActions">json data.</param>
        /// <returns>成功則回傳留言列表，失敗則回傳null</returns>
        private List<CommentData> ParseComment(dynamic commentActions)
        {
            if (commentActions == null)
            {
                return null;
            }

            List<CommentData> ret = new List<CommentData>();
            for (int i = 0; i < commentActions.Count; i++)
            {
                CommentData cmt = new CommentData();
                cmt.addChatItemAction.clientId = Convert.ToString(JsonHelper.TryGetValueByXPath(commentActions[i],
                    "addChatItemAction.clientId", ""));
                cmt.replaceChatItemAction.targetItemId = Convert.ToString(JsonHelper.TryGetValueByXPath(commentActions[i],
                    "replaceChatItemAction.targetItemId", ""));

                var txtMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                    "addChatItemAction.item.liveChatTextMessageRenderer", null);
                if (txtMsgRd != null)
                {
                    ParseTextMessage(cmt.addChatItemAction.item.liveChatTextMessageRenderer, txtMsgRd);
                }
                else
                {
                    dynamic paidMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                        "addChatItemAction.item.liveChatPaidMessageRenderer", null);
                    if (paidMsgRd != null)
                    {
                        ParsePaidMessage(cmt.addChatItemAction.item.liveChatPaidMessageRenderer, paidMsgRd);
                    }
                    else
                    {
                        dynamic membershipItmRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                            "addChatItemAction.item.liveChatMembershipItemRenderer", null);
                        if (membershipItmRd != null)
                        {
                            ParseMembershipItem(cmt.addChatItemAction.item.liveChatMembershipItemRenderer, membershipItmRd);
                        }
                        else
                        {
                            dynamic giftRedemptionAcmRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                                "addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer", null);
                            if (giftRedemptionAcmRd != null)
                            {
                                ParseGiftRedemptionAnnouncement(cmt.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer, giftRedemptionAcmRd);
                            }
                            else
                            {
                                ///continue;
                                dynamic giftPurchaseAcmRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                                    "addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer", null);
                                if (giftPurchaseAcmRd != null)
                                {
                                    ParseGiftPurchaseAnnouncement(cmt.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer, giftPurchaseAcmRd);
                                }
                                else
                                {
                                    dynamic paidStkRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                                        "addChatItemAction.item.liveChatPaidStickerRenderer", null);
                                    if (paidStkRd != null)
                                    {
                                        ParsePaidSticker(cmt.addChatItemAction.item.liveChatPaidStickerRenderer, paidStkRd);
                                    }
                                    else
                                    {
                                        dynamic replacetxtMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i],
                                            "replaceChatItemAction.replacementItem.liveChatTextMessageRenderer", null);
                                        if (replacetxtMsgRd != null)
                                        {
                                            ParseTextMessage(cmt.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer, replacetxtMsgRd);
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }

                ret.Add(cmt);
            }
            return ret;
        }

        /// <summary>
        /// 解析付費留言資訊
        /// </summary>
        /// <param name="liveChatPaidMessageRenderer">付費留言</param>
        /// <param name="paidMsgRd">json data.</param>
        private void ParsePaidMessage(LiveChatPaidMessageRenderer liveChatPaidMessageRenderer, dynamic paidMsgRd)
        {
            //解析留言內容
            ParseTextMessage(liveChatPaidMessageRenderer, paidMsgRd);

            //解析付費留言內容
            liveChatPaidMessageRenderer.purchaseAmountText.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(paidMsgRd, "purchaseAmountText.simpleText", ""));
            liveChatPaidMessageRenderer.headerBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerBackgroundColor", 0));
            liveChatPaidMessageRenderer.headerTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerTextColor", 0));
            liveChatPaidMessageRenderer.bodyBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyBackgroundColor", 0));
            liveChatPaidMessageRenderer.bodyTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyTextColor", 0));
            liveChatPaidMessageRenderer.authorNameTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "authorNameTextColor", 0));
            liveChatPaidMessageRenderer.timestampColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "timestampColor", 0));
        }

        /// <summary>
        /// 解析付費貼圖資訊
        /// </summary>
        /// <param name="liveChatPaidStickerRenderer">付費貼圖</param>
        /// <param name="paidStkRd">json data.</param>
        private void ParsePaidSticker(LiveChatPaidStickerRenderer liveChatPaidStickerRenderer, dynamic paidStkRd)
        {
            //解析留言內容
            ParseTextMessage(liveChatPaidStickerRenderer, paidStkRd);

            //解析付費貼圖內容
            liveChatPaidStickerRenderer.purchaseAmountText.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(paidStkRd, "purchaseAmountText.simpleText", ""));
            liveChatPaidStickerRenderer.backgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStkRd, "backgroundColor", 0));
            liveChatPaidStickerRenderer.moneyChipBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStkRd, "moneyChipBackgroundColor", 0));
            liveChatPaidStickerRenderer.moneyChipTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStkRd, "moneyChipTextColor", 0));
            liveChatPaidStickerRenderer.authorNameTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStkRd, "authorNameTextColor", 0));
            liveChatPaidStickerRenderer.stickerDisplayWidth = Convert.ToInt32(JsonHelper.TryGetValueByXPath(paidStkRd, "stickerDisplayWidth", 0));
            liveChatPaidStickerRenderer.stickerDisplayHeight = Convert.ToInt32(JsonHelper.TryGetValueByXPath(paidStkRd, "stickerDisplayHeight", 0));

            //解析貼圖內容
            dynamic sticker_data = JsonHelper.TryGetValueByXPath(paidStkRd, "sticker");
            if (sticker_data != null)
            {
                liveChatPaidStickerRenderer.sticker = ParseSticker(sticker_data);
            }
        }

        /// <summary>
        /// 解析贈禮會員資訊
        /// </summary>
        /// <param name="liveChatSponsorshipsHeaderRenderer">贈禮會員</param>
        /// <param name="sponsorshipsHedRd">json data.</param>
        private void ParseHeader(LiveChatSponsorshipsHeaderRenderer liveChatSponsorshipsHeaderRenderer, dynamic sponsorshipsHedRd)
        {
            liveChatSponsorshipsHeaderRenderer.authorName.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "authorName.simpleText", ""));
            liveChatSponsorshipsHeaderRenderer.authorPhoto.thumbnails = ParseAuthorPhotoThumb(JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "authorPhoto.thumbnails", null));
            liveChatSponsorshipsHeaderRenderer.contextMenuAccessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "contextMenuAccessibility.accessibilityData.label", ""));

            //解析會員贈禮內容
            dynamic runs = JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "primaryText.runs");
            if (runs != null)
            {
                for (int i = 0; i < runs.Count; i++)
                {
                    dynamic run = runs[i];
                    Runs r = new Runs();

                    //解析一般文字元素
                    string text = ParseText(run);
                    if (text != "")
                    {
                        r.text = text;
                        liveChatSponsorshipsHeaderRenderer.primaryText.runs.Add(r);
                    }

                    //解析Emoji元素
                    Emoji emj = ParseEmoji(run);
                    if (emj != null)
                    {
                        r.emoji = emj;
                        liveChatSponsorshipsHeaderRenderer.primaryText.runs.Add(r);
                    }
                }
            }
            else
            {
                liveChatSponsorshipsHeaderRenderer.primaryText.simpleText = "";
            }

            var authorBadges = JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "authorBadges", null);
            if (authorBadges != null)
            {
                //留言者可能擁有多個徽章 (EX:管理員、會員)
                for (int i = 0; i < authorBadges.Count; i++)
                {
                    AuthorBadge badge = new AuthorBadge();
                    badge.tooltip = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.tooltip"));
                    liveChatSponsorshipsHeaderRenderer.authorBadges.Add(badge);
                }
            }

            dynamic thumbsObj = JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "image.thumbnails");
            for (int i = 0; i < thumbsObj.Count; i++)
            {
                Thumbnails thumbs = new Thumbnails();
                thumbs.url = Convert.ToString(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.url"));
                thumbs.width = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.width", 0));
                thumbs.height = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.height", 0));

                liveChatSponsorshipsHeaderRenderer.image.thumbnails.Add(thumbs);
            }

            liveChatSponsorshipsHeaderRenderer.image.accessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(sponsorshipsHedRd, "image.accessibility.accessibilityData.label", ""));
        }

        /// <summary>
        /// 解析會員贈禮通知
        /// </summary>
        /// <param name="liveChatSponsorshipsGiftPurchaseAnnouncementRenderer">會員贈禮通知</param>
        /// <param name="giftPurchaseAcmRd">json data.</param>
        private void ParseGiftPurchaseAnnouncement(LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer liveChatSponsorshipsGiftPurchaseAnnouncementRenderer, dynamic giftPurchaseAcmRd)
        {
            liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.id = Convert.ToString(JsonHelper.TryGetValueByXPath(giftPurchaseAcmRd, "id", ""));
            liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.timestampUsec = Convert.ToInt64(JsonHelper.TryGetValueByXPath(giftPurchaseAcmRd, "timestampUsec", 0));
            liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.authorExternalChannelId = Convert.ToString(JsonHelper.TryGetValueByXPath(giftPurchaseAcmRd, "authorExternalChannelId", ""));

            dynamic sponsorshipsHedRd = JsonHelper.TryGetValueByXPath(giftPurchaseAcmRd, "header.liveChatSponsorshipsHeaderRenderer", null);
            if (sponsorshipsHedRd != null)
            {
                ParseHeader(liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer, sponsorshipsHedRd);
            }
            else
            {
                liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorName.simpleText = "";
                liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorPhoto.thumbnails = null;
                liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.simpleText = "";
                liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorBadges = null;
                liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.contextMenuAccessibility.accessibilityData.label = "";
            }
        }

        /// <summary>
        /// 解析獲得會員贈禮通知
        /// </summary>
        /// <param name="liveChatSponsorshipsGiftRedemptionAnnouncementRenderer">獲得會員贈禮通知</param>
        /// <param name="giftRedemptionAcmRd">json data.</param>
        private void ParseGiftRedemptionAnnouncement(LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer liveChatSponsorshipsGiftRedemptionAnnouncementRenderer, dynamic giftRedemptionAcmRd)
        {
            //解析留言內容
            ParseTextMessage(liveChatSponsorshipsGiftRedemptionAnnouncementRenderer, giftRedemptionAcmRd);
        }

        /// <summary>
        /// 解析會員訊息資訊
        /// </summary>
        /// <param name="liveChatMembershipItemRenderer">會員訊息</param>
        /// <param name="membershipMsgRd">json data.</param>
        private void ParseMembershipItem(LiveChatMembershipItemRenderer liveChatMembershipItemRenderer, dynamic membershipItmRd)
        {
            //解析留言內容
            ParseTextMessage(liveChatMembershipItemRenderer, membershipItmRd);

            //解析會員訊息內容 新會員有runs 其餘則為會員名稱
            dynamic subruns = JsonHelper.TryGetValueByXPath(membershipItmRd, "headerSubtext.runs");
            if (subruns != null)
            {
                string subText = "";
                for (int i = 0; i < subruns.Count; i++)
                {
                    dynamic run = subruns[i];
                    Runs r = new Runs();

                    //解析一般文字元素
                    string text = ParseText(run);
                    if (text != "")
                    {
                        r.text = text;
                        liveChatMembershipItemRenderer.headerSubtext.runs.Add(r);
                    }

                    //解析Emoji元素
                    Emoji emj = ParseEmoji(run);
                    if (emj != null)
                    {
                        r.emoji = emj;
                        liveChatMembershipItemRenderer.headerSubtext.runs.Add(r);
                    }
                }
                subText = FormatHeaderSubText(liveChatMembershipItemRenderer.headerSubtext);
                liveChatMembershipItemRenderer.headerSubtext.simpleText = subText;
                liveChatMembershipItemRenderer.headerPrimaryText.simpleText = "";
            }
            else
            {
                liveChatMembershipItemRenderer.headerSubtext.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(membershipItmRd, "headerSubtext.simpleText", ""));
                //會員訊息包含自訂表情符號或空格時runs陣列會分割成多元素
                dynamic runs = JsonHelper.TryGetValueByXPath(membershipItmRd, "headerPrimaryText.runs");
                if (runs != null)
                {
                    string primaryText = "";
                    for (int i = 0; i < runs.Count; i++)
                    {
                        dynamic run = runs[i];
                        Runs r = new Runs();

                        //解析一般文字元素
                        string text = ParseText(run);
                        if (text != "")
                        {
                            r.text = text;
                            liveChatMembershipItemRenderer.headerPrimaryText.runs.Add(r);
                        }

                        //解析Emoji元素
                        Emoji emj = ParseEmoji(run);
                        if (emj != null)
                        {
                            r.emoji = emj;
                            liveChatMembershipItemRenderer.headerPrimaryText.runs.Add(r);
                        }
                    }
                    primaryText = FormatHeaderPrimaryText(liveChatMembershipItemRenderer.headerPrimaryText);
                    liveChatMembershipItemRenderer.headerPrimaryText.simpleText = primaryText;
                }
                else
                    liveChatMembershipItemRenderer.headerPrimaryText.simpleText = "";
            }
        }

        /// <summary>
        /// 解析留言內容
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <param name="txtMsgRd">json data.</param>
        private void ParseTextMessage(LiveChatTextMessageRenderer liveChatTextMessageRenderer, dynamic txtMsgRd)
        {
            liveChatTextMessageRenderer.authorExternalChannelId = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorExternalChannelId", ""));
            liveChatTextMessageRenderer.authorName.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorName.simpleText", ""));
            liveChatTextMessageRenderer.authorPhoto.thumbnails = ParseAuthorPhotoThumb(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorPhoto.thumbnails", null));
            liveChatTextMessageRenderer.contextMenuAccessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "contextMenuAccessibility.accessibilityData.label", ""));
            liveChatTextMessageRenderer.id = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "id", ""));
            liveChatTextMessageRenderer.timestampUsec = Convert.ToInt64(JsonHelper.TryGetValueByXPath(txtMsgRd, "timestampUsec", 0));

            //留言包含自訂表情符號或空格時runs陣列會分割成多元素
            dynamic runs = JsonHelper.TryGetValueByXPath(txtMsgRd, "message.runs");
            if (runs != null)
            {
                for (int i = 0; i < runs.Count; i++)
                {
                    dynamic run = runs[i];
                    Runs r = new Runs();

                    //解析一般文字元素
                    string text = ParseText(run);
                    if (text != "")
                    {
                        r.text = text;
                        liveChatTextMessageRenderer.message.runs.Add(r);
                    }

                    //解析Emoji元素
                    Emoji emj = ParseEmoji(run);
                    if (emj != null)
                    {
                        r.emoji = emj;
                        liveChatTextMessageRenderer.message.runs.Add(r);
                    }
                }
            }
            else
                liveChatTextMessageRenderer.message.simpleText = "";

            var authorBadges = JsonHelper.TryGetValueByXPath(txtMsgRd, "authorBadges", null);
            if (authorBadges != null)
            {
                //留言者可能擁有多個徽章 (EX:管理員、會員)
                for (int i = 0; i < authorBadges.Count; i++)
                {
                    AuthorBadge badge = new AuthorBadge();
                    badge.tooltip = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.tooltip"));
                    liveChatTextMessageRenderer.authorBadges.Add(badge);
                }
            }
        }

        /// <summary>
        /// 解析留言者縮圖
        /// </summary>
        /// <param name="authorPhotoData">json data.</param>
        private List<Thumbnails> ParseAuthorPhotoThumb(dynamic authorPhotoData)
        {
            if (authorPhotoData == null)
            {
                return null;
            }

            List<Thumbnails> ret = new List<Thumbnails>();

            for (int i = 0; i < authorPhotoData.Count; i++)
            {
                Thumbnails thumb = new Thumbnails();
                thumb.url = JsonHelper.TryGetValue(authorPhotoData[i], "url", "");
                thumb.width = JsonHelper.TryGetValue(authorPhotoData[i], "width", "");
                thumb.height = JsonHelper.TryGetValue(authorPhotoData[i], "height", "");
                ret.Add(thumb);
            }

            return ret;
        }

        /// <summary>
        /// 解析一般文字元素
        /// </summary>
        /// <param name="run">json data</param>
        /// <returns>回傳留言文字。若json data內非一般文字則回傳空字串</returns>
        private string ParseText(dynamic run)
        {
            string xPath = "text";
            return Convert.ToString(JsonHelper.TryGetValueByXPath(run, xPath, ""));
        }

        /// <summary>
        /// 解析Emoji元素
        /// </summary>
        /// <param name="run">json data</param>
        /// <returns>回傳留言的Emoji物件。若Json data內非Emoji則回傳null</returns>
        private Emoji ParseEmoji(dynamic run)
        {
            Emoji ret = new Emoji();
            dynamic emojiObj = JsonHelper.TryGetValue(run, "emoji");

            if (emojiObj == null)
                return null;

            ret.emojiId = Convert.ToString(JsonHelper.TryGetValue(emojiObj, "emojiId", ""));

            dynamic shortcuts = JsonHelper.TryGetValue(emojiObj, "shortcuts");
            for (int i = 0; i < shortcuts.Count; i++)
            {
                ret.shortcuts.Add(Convert.ToString(shortcuts[i]));
            }

            dynamic searchTerms = JsonHelper.TryGetValue(emojiObj, "searchTerms");
            for (int i = 0; i < searchTerms.Count; i++)
            {
                ret.searchTerms.Add(Convert.ToString(searchTerms[i]));
            }

            ret.isCustomEmoji = Convert.ToBoolean(JsonHelper.TryGetValue(emojiObj, "isCustomEmoji", false));

            dynamic thumbsObj = JsonHelper.TryGetValueByXPath(emojiObj, "image.thumbnails");
            for (int i = 0; i < thumbsObj.Count; i++)
            {
                Thumbnails thumbs = new Thumbnails();
                thumbs.url = Convert.ToString(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.url"));
                thumbs.width = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.width", 0));
                thumbs.height = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.height", 0));

                ret.image.thumbnails.Add(thumbs);
            }

            ret.image.accessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(emojiObj, "image.accessibility.accessibilityData.label", ""));

            return ret;
        }

        /// <summary>
        /// 解析Sticker元素
        /// </summary>
        /// <param name="sticker_data">json data</param>
        /// <returns>回傳留言的Sticker物件。若Json data內非Sticker則回傳null</returns>
        private Sticker ParseSticker(dynamic sticker_data)
        {
            Sticker ret = new Sticker();
            dynamic thumbsObj = JsonHelper.TryGetValueByXPath(sticker_data, "thumbnails");
            for (int i = 0; i < thumbsObj.Count; i++)
            {
                Thumbnails thumbs = new Thumbnails();
                thumbs.url = $"https:{Convert.ToString(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.url"))}";
                thumbs.width = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.width", 0));
                thumbs.height = Convert.ToInt32(JsonHelper.TryGetValueByXPath(thumbsObj, $"{i.ToString()}.height", 0));

                ret.thumbnails.Add(thumbs);
            }

            ret.accessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(sticker_data, "accessibility.accessibilityData.label", "???"));

            return ret;
        }

        private string FormatHeaderSubText(HeaderSubtext headerSubtext)
        {
            string ret = "";
            for (int i = 0; i < headerSubtext.runs.Count; i++)
            {
                Runs r = headerSubtext.runs[i];
                ret += r.text;
                ret += FormatEmojiImage(r.emoji);
            }

            return ret;
        }

        private string FormatHeaderPrimaryText(HeaderPrimaryText headerPrimaryText)
        {
            string ret = "";
            for (int i = 0; i < headerPrimaryText.runs.Count; i++)
            {
                Runs r = headerPrimaryText.runs[i];
                ret += r.text;
                ret += FormatEmojiImage(r.emoji);
            }

            return ret;
        }

        private string FormatEmojiImage(Emoji emoji)
        {
            if (emoji == null)
            {
                return "";
            }

            string ret = "";
            if (emoji.isCustomEmoji)
            {
                Thumbnails thumb = emoji.image.thumbnails.ElementAtOrDefault(0);
                if (thumb != null)
                {
                    string url = thumb.url;
                    int w = thumb.width;
                    int h = thumb.height;

                    ret = $"[img source='{url}' width={w} height={h}]";
                }
            }
            else
            {
                ret = emoji.emojiId;
            }

            return ret;
        }

        #endregion Private Method

        #region Event

        public delegate void ErrorHandleMethod(CommentLoader sender, CommentLoaderErrorCode errCode, object obj);

        /// <summary>
        /// CommentLoader發生錯誤事件
        /// </summary>
        public event ErrorHandleMethod OnError;

        /// <summary>
        /// 發出錯誤事件
        /// </summary>
        /// <param name="errCode">錯誤碼</param>
        /// <param name="obj">附帶的錯誤資訊</param>
        private void RaiseError(CommentLoaderErrorCode errCode, object obj)
        {
            Debug.WriteLine(String.Format("[RaiseError] errCode:{0}, {1}", errCode.ToString(), obj));
            if (OnError != null)
            {
                OnError(this, errCode, obj);
            }
        }

        public delegate void CommentsReceiveMethod(CommentLoader sender, List<CommentData> lsComments);

        /// <summary>
        /// CommentLoader取得新留言事件
        /// </summary>
        public event CommentsReceiveMethod OnCommentsReceive;

        /// <summary>
        /// 發出收到留言事件
        /// </summary>
        /// <param name="lsComments">收到的留言資料列表</param>
        private void RaiseCommentsReceive(List<CommentData> lsComments)
        {
            if (OnCommentsReceive != null)
            {
                OnCommentsReceive(this, lsComments);
            }
        }

        public delegate void StatusChangedMethod(CommentLoader sender, CommentLoaderStatus status);

        /// <summary>
        /// CommentLoader執行時發生的各階段事件
        /// <para>GetComments狀態會持續發生</para>
        /// </summary>
        public event StatusChangedMethod OnStatusChanged;

        /// <summary>
        /// 發出執行階段狀態事件
        /// </summary>
        /// <param name="status">正在執行的狀態</param>
        private void RaiseStatusChanged(CommentLoaderStatus status)
        {
            this.Status = status;
            //Debug.WriteLine(String.Format("[OnStatusChanged] {0}", this.Status.ToString()));
            if (OnStatusChanged != null)
            {
                OnStatusChanged(this, status);
            }
        }

        #endregion Event
    }
}
