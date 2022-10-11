using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KomeTube.Common;
using KomeTube.Kernel.YtLiveChatDataModel;

namespace KomeTube.ViewModel
{
    public class CommentVM : ViewModelBase
    {
        #region Private Member

        private CommentData _data;
        private DateTime _dateTime;
        private bool _isEnableCopyMessage;

        #endregion Private Member

        #region Constructor

        public CommentVM(CommentData data)
        {
            _data = data;

            this.IsEnableCopyMessage = true;
        }

        #endregion Constructor

        #region Public Member

        /// <summary>
        /// 取得留言時間
        /// <para>若要使用格式化後的留言時間字串請使用DateTimeText</para>
        /// </summary>
        public DateTime Date
        {
            get
            {
                if (DateTime.MinValue != _dateTime)
                {
                    return _dateTime;
                }

                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatPaidMessageRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else if (_data.addChatItemAction.item.IsNewMembership || _data.addChatItemAction.item.IsMembershipItem)
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatMembershipItemRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatPaidStickerRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    double timeStamp = (double)_data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }
                else
                {
                    double timeStamp = (double)_data.addChatItemAction.item.liveChatTextMessageRenderer.timestampUsec / 1000000.0;
                    _dateTime = new DateTime(1970, 1, 1).AddSeconds(timeStamp).ToLocalTime();
                }

                return _dateTime;
            }
        }

        /// <summary>
        /// 取得格式化後留言時間字串
        /// </summary>
        public String DateTimeText
        {
            get
            {
                return this.Date.ToString("HH:mm:ss");
            }
        }

        /// <summary>
        /// 取得留言者名稱
        /// </summary>
        public String AuthorName
        {
            get
            {
                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    return _data.addChatItemAction.item.liveChatPaidMessageRenderer.authorName.simpleText;
                }
                else if (_data.addChatItemAction.item.IsNewMembership || _data.addChatItemAction.item.IsMembershipItem)
                {
                    return _data.addChatItemAction.item.liveChatMembershipItemRenderer.authorName.simpleText;
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.authorName.simpleText;
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorName.simpleText;
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    return _data.addChatItemAction.item.liveChatPaidStickerRenderer.authorName.simpleText;
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    return _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorName.simpleText;
                }
                else
                {
                    return _data.addChatItemAction.item.liveChatTextMessageRenderer.authorName.simpleText;
                }
            }

            set
            {
            }
        }

        /// <summary>
        /// 取得留言者徽章稱號
        /// </summary>
        public String AuthorBadges
        {
            get
            {
                string ret = "";

                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatPaidMessageRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else if (_data.addChatItemAction.item.IsNewMembership || _data.addChatItemAction.item.IsMembershipItem)
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatMembershipItemRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatPaidStickerRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    foreach (var badge in _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }
                else
                {
                    foreach (var badge in _data.addChatItemAction.item.liveChatTextMessageRenderer.authorBadges)
                    {
                        ret += badge.tooltip + " ";
                    }
                }

                return ret;
            }
        }

        /// <summary>
        /// 取得留言內容
        /// <para>若是付費留言則會顯示為"¥{金額} {留言內容}"</para>
        /// <para>若是新會員訊息則會顯示為"{加入訊息}"</para>
        /// <para>若是里程碑訊息則會顯示為"[{里程碑} {會員名稱}] {留言內容}"</para>
        /// <para>若是獲得贈送會員通知則會顯示不變</para>
        /// <para>若是付費貼圖則會顯示為"¥{金額} {貼圖內容}"</para>
        /// </summary>
        public String Message
        {
            get
            {
                string msgText = "";
                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    msgText = FormatMessageText(_data.addChatItemAction.item.liveChatPaidMessageRenderer.message);
                    _data.addChatItemAction.item.liveChatPaidMessageRenderer.message.simpleText = msgText;

                    return String.Format("{0} {1}",
                        _data.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText,
                        _data.addChatItemAction.item.liveChatPaidMessageRenderer.message.simpleText);
                }
                else if (_data.addChatItemAction.item.IsNewMembership)
                {
                    return Membership;
                }
                else if (_data.addChatItemAction.item.IsMembershipItem)
                {
                    msgText = FormatMessageText(_data.addChatItemAction.item.liveChatMembershipItemRenderer.message);
                    _data.addChatItemAction.item.liveChatMembershipItemRenderer.message.simpleText = msgText;
                    
                    return String.Format("{0} {1}",
                        Membership,
                        _data.addChatItemAction.item.liveChatMembershipItemRenderer.message.simpleText);
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    return GiftRedemption;
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    return String.Format("{0} {1}",
                        _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorName.simpleText,
                        GiftPurchase);
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    msgText = FormatStickerImage(_data.addChatItemAction.item.liveChatPaidStickerRenderer.sticker);

                    return String.Format("{0} {1}",
                        _data.addChatItemAction.item.liveChatPaidStickerRenderer.purchaseAmountText.simpleText,
                        msgText);
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    msgText = FormatMessageText(_data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.message);
                    _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.message.simpleText = msgText;

                    return _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.message.simpleText;
                }
                else
                {
                    msgText = FormatMessageText(_data.addChatItemAction.item.liveChatTextMessageRenderer.message);
                    _data.addChatItemAction.item.liveChatTextMessageRenderer.message.simpleText = msgText;

                    return _data.addChatItemAction.item.liveChatTextMessageRenderer.message.simpleText;
                }
            }
        }

        /// <summary>
        /// 留言內容全文字版，表情符號改由shortcut表示，而非圖片網址
        /// </summary>
        public string ContentMessage
        {
            get
            {
                string content = "";
                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    content = FormatContentMessage(_data.addChatItemAction.item.liveChatPaidMessageRenderer.message);

                    return String.Format("{0} {1}",
                        _data.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText,
                        content);
                }
                else
                {
                    content = FormatContentMessage(_data.addChatItemAction.item.liveChatTextMessageRenderer.message);

                    return content;
                }
            }
        }

        /// <summary>
        /// 取得付費金額(包含貨幣符號)，若非付費留言則回傳null
        /// </summary>
        public String PaidMessage
        {
            get
            {
                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    return _data.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得付費金額(包含貨幣符號)，若非付費貼圖則回傳null
        /// </summary>
        public String PaidSticker
        {
            get
            {
                if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    return _data.addChatItemAction.item.liveChatPaidStickerRenderer.purchaseAmountText.simpleText;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得留言者頭像網址
        /// </summary>
        public String AuthorPhotoUrl
        {
            get
            {
                //thumbnails[0]: 32*32 size
                //thumbnails[1]: 64*64 size

                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    return _data.addChatItemAction.item.liveChatPaidMessageRenderer.authorPhoto.thumbnails[0].url;
                }
                else if (_data.addChatItemAction.item.IsNewMembership || _data.addChatItemAction.item.IsMembershipItem)
                {
                    return _data.addChatItemAction.item.liveChatMembershipItemRenderer.authorPhoto.thumbnails[0].url;
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.authorPhoto.thumbnails[0].url;
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.authorPhoto.thumbnails[0].url;
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    return _data.addChatItemAction.item.liveChatPaidStickerRenderer.authorPhoto.thumbnails[0].url;
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    return _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorPhoto.thumbnails[0].url;
                }
                else
                {
                    return _data.addChatItemAction.item.liveChatTextMessageRenderer.authorPhoto.thumbnails[0].url;
                }
            }
        }

        /// <summary>
        /// 取得留言者ID
        /// </summary>
        public String AuthorID
        {
            get
            {
                if (_data.addChatItemAction.item.IsPaidMessage)
                {
                    return _data.addChatItemAction.item.liveChatPaidMessageRenderer.authorExternalChannelId;
                }
                else if (_data.addChatItemAction.item.IsNewMembership || _data.addChatItemAction.item.IsMembershipItem)
                {
                    return _data.addChatItemAction.item.liveChatMembershipItemRenderer.authorExternalChannelId;
                }
                else if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.authorExternalChannelId;
                }
                else if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.authorExternalChannelId;
                }
                else if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    return _data.addChatItemAction.item.liveChatPaidStickerRenderer.authorExternalChannelId;
                }
                else if (_data.replaceChatItemAction.replacementItem.IsReplace)
                {
                    return _data.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorExternalChannelId;
                }
                else
                {
                    return _data.addChatItemAction.item.liveChatTextMessageRenderer.authorExternalChannelId;
                }
            }
        }

        /// <summary>
        /// 取得會員訊息
        /// </summary>
        public String Membership
        {
            get
            {
                if (_data.addChatItemAction.item.IsNewMembership)
                {
                    return _data.addChatItemAction.item.liveChatMembershipItemRenderer.headerSubtext.simpleText;
                }
                else if (_data.addChatItemAction.item.IsMembershipItem)
                {
                    return String.Format("{0} {1}",
                        _data.addChatItemAction.item.liveChatMembershipItemRenderer.headerPrimaryText.simpleText,
                        _data.addChatItemAction.item.liveChatMembershipItemRenderer.headerSubtext.simpleText);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得獲得贈禮通知
        /// </summary>
        public String GiftRedemption
        {
            get
            {
                if (_data.addChatItemAction.item.IsGiftRedemption)
                {
                    String msgText = FormatMessageText(_data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message);
                    _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message.simpleText = msgText;

                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message.simpleText;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得贈禮通知
        /// </summary>
        public String GiftPurchase
        {
            get
            {
                if (_data.addChatItemAction.item.IsGiftPurchase)
                {
                    String primaryText = FormatPrimaryText(_data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText);
                    _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.simpleText = primaryText;

                    return _data.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.simpleText;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得貼圖內容
        /// </summary>
        public String StickerLabel
        {
            get
            {
                if (_data.addChatItemAction.item.IsPaidSticker)
                {
                    return _data.addChatItemAction.item.liveChatPaidStickerRenderer.sticker.accessibility.accessibilityData.label;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 取得留言者頻道連結
        /// </summary>
        public String AuthorChannelUrl
        {
            get
            {
                string url = @"https://www.youtube.com/channel/" + this.AuthorID;
                return url;
            }
        }

        public bool IsEnableCopyMessage
        {
            get
            {
                return _isEnableCopyMessage;
            }
            set
            {
                if (_isEnableCopyMessage != value)
                {
                    _isEnableCopyMessage = value;
                    OnPropertyChanged(nameof(this.IsEnableCopyMessage));
                }
            }
        }

        #endregion Public Member

        #region Command

        private CommandBase _cmdOpenAuthorChannelUrl;

        public CommandBase CmdOpenAuthorChannelUrl
        {
            get
            {
                return _cmdOpenAuthorChannelUrl ?? (_cmdOpenAuthorChannelUrl = new CommandBase(x => OpenAuthorChannelUrl()));
            }
        }

        private CommandBase _cmdCopyContentMessage;

        public CommandBase CmdCopyContentMessage
        {
            get
            {
                return _cmdCopyContentMessage ?? (_cmdCopyContentMessage = new CommandBase(x => CopyContentMessage()));
            }
        }

        public Func<String, CommentVM, bool> CommandAction;

        private void ExecuteCommand(String cmd)
        {
            if (this.CommandAction != null)
            {
                this.CommandAction(cmd, this);
            }
        }

        #endregion Command

        #region Private Method

        private string FormatMessageText(Message msg)
        {
            string ret = "";
            for (int i = 0; i < msg.runs.Count; i++)
            {
                Runs r = msg.runs[i];
                ret += r.text;
                ret += FormatEmojiImage(r.emoji);
            }

            return ret;
        }

//        private string FormatPrimaryText(PrimaryText primaryText)
//        {
//            string ret = "";
//            for (int i = 0; i < primaryText.runs.Count; i++)
//            {
//                Runs r = primaryText.runs[i];
//                ret += r.text;
//                ret += FormatEmojiImage(r.emoji);
//            }
//
//            return ret;
//        }

        private string FormatContentMessage(Message msg)
        {
            string ret = "";
            for (int i = 0; i < msg.runs.Count; i++)
            {
                Runs r = msg.runs[i];
                ret += r.text;
                if (r.emoji.shortcuts.Count > 0)
                {
                    //判斷表情符號類型
                    if (r.emoji.isCustomEmoji
                        && r.emoji.shortcuts.Count >= 2)
                    {
                        //頻道自訂表符
                        ret += r.emoji.shortcuts[1];
                    }
                    else if (r.emoji.isCustomEmoji
                        && r.emoji.shortcuts.Count < 2)
                    {
                        //YT通用自訂表符
                        ret += r.emoji.shortcuts[0];
                    }
                    else
                    {
                        //文字符號表符
                        ret += r.emoji.emojiId;
                    }
                }
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
                    string label = emoji.image.accessibility.accessibilityData.label;

                    ret = $"[img title='{label}' source='{url}' width={w} height={h}]"; // 但title似乎沒有效果
                }
            }
            else
            {
                ret = emoji.emojiId;
            }

            return ret;
        }

        private string FormatStickerImage(Sticker sticker)
        {
            if (sticker == null)
            {
                return "";
            }

            string ret = "";
            Thumbnails thumb = sticker.thumbnails.ElementAtOrDefault(0);
            if (thumb != null)
            {
                string url = thumb.url;
                int w = thumb.width;
                if (w > 40)
                {
                    w = 40;
                }
                int h = thumb.height;
                if (h > 40)
                {
                    h = 40;
                }
                string label = sticker.accessibility.accessibilityData.label;

                ret = $"[img title='{label}' source='{url}' width={w} height={h}]";
            }

            return ret;
        }

        private void OpenAuthorChannelUrl()
        {
            System.Diagnostics.Process.Start(this.AuthorChannelUrl);
        }

        private void CopyContentMessage()
        {
            try
            {
                Clipboard.SetText(this.ContentMessage);
                this.IsEnableCopyMessage = false;
                Task.Run(() =>
                {
                    SpinWait.SpinUntil(() => false, 500);
                    this.IsEnableCopyMessage = true;
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        #endregion Private Method
    }
}
