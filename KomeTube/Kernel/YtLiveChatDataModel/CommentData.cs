using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KomeTube.Kernel.YtLiveChatDataModel
{
    public class Thumbnails
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Accessibility
    {
        public Accessibility()
        {
            this.accessibilityData = new AccessibilityData();
        }

        public AccessibilityData accessibilityData { get; set; }
    }

    public class Image
    {
        public Image()
        {
            this.thumbnails = new List<Thumbnails>();
            this.accessibility = new Accessibility();
        }

        public List<Thumbnails> thumbnails { get; set; }
        public Accessibility accessibility { get; set; }
    }

    public class Sticker
    {
        public Sticker()
        {
            this.thumbnails = new List<Thumbnails>();
            this.accessibility = new Accessibility();
        }

        public List<Thumbnails> thumbnails { get; set; }
        public Accessibility accessibility { get; set; }
    }

    public class Emoji
    {
        public Emoji()
        {
            this.shortcuts = new List<string>();
            this.searchTerms = new List<string>();
            this.image = new Image();
            this.isCustomEmoji = false;
        }

        public string emojiId { get; set; }
        public List<string> shortcuts { get; set; }
        public List<string> searchTerms { get; set; }
        public Image image { get; set; }
        public bool isCustomEmoji { get; set; }
    }

    public class Runs
    {
        public Runs()
        {
            this.emoji = new Emoji();
        }

        public string text { get; set; }

        public Emoji emoji { get; set; }
    }

    public class Message
    {
        public Message()
        {
            this.runs = new List<Runs>();
        }

        public string simpleText { get; set; }
        public List<Runs> runs { get; set; }
    }

    public class AuthorName
    {
        public string simpleText { get; set; }
    }

    public class AuthorBadge
    {
        public String tooltip { get; set; }
    }

//    與 Thumbnails 相同，所以刪除了，並把 AuthorPhoto 的改為 Thumbnails
//    public class Thumbnail
//    {
//        public string url { get; set; }
//        public int width { get; set; }
//        public int height { get; set; }
//    }

    public class AuthorPhoto
    {
        public AuthorPhoto()
        {
            this.thumbnails = new List<Thumbnails>();
        }

        public List<Thumbnails> thumbnails { get; set; }
    }

    public class PrimaryText
    {
        public PrimaryText()
        {
            this.runs = new List<Runs>();
        }

        public string simpleText { get; set; }
        public List<Runs> runs { get; set; }
    }

    public class WebCommandMetadata
    {
        public bool ignoreNavigation { get; set; }
    }

    public class CommandMetadata
    {
        public CommandMetadata()
        {
            this.webCommandMetadata = new WebCommandMetadata();
        }

        public WebCommandMetadata webCommandMetadata { get; set; }
    }

    public class LiveChatItemContextMenuEndpoint
    {
        public string parameters { get; set; }
    }

    public class ContextMenuEndpoint
    {
        public ContextMenuEndpoint()
        {
            this.commandMetadata = new CommandMetadata();
            this.liveChatItemContextMenuEndpoint = new LiveChatItemContextMenuEndpoint();
        }

        public string clickTrackingParams { get; set; }
        public CommandMetadata commandMetadata { get; set; }
        public LiveChatItemContextMenuEndpoint liveChatItemContextMenuEndpoint { get; set; }
    }

    public class AccessibilityData
    {
        public string label { get; set; }
    }

    public class PurchaseAmountText
    {
        public string simpleText { get; set; }
    }

    public class HeaderPrimaryText
    {
        public HeaderPrimaryText()
        {
            this.runs = new List<Runs>();
        }

        public string simpleText { get; set; }
        public List<Runs> runs { get; set; }
    }

    public class HeaderSubtext
    {
        public HeaderSubtext()
        {
            this.runs = new List<Runs>();
        }

        public string simpleText { get; set; }
        public List<Runs> runs { get; set; }
    }

    public class LiveChatSponsorshipsHeaderRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatSponsorshipsHeaderRenderer()
        {
            this.primaryText = new PrimaryText();
            this.image = new Image();
        }

        public PrimaryText primaryText { get; set; }
        public Image image { get; set; }
    }

    public class Header
    {
        public Header()
        {
            this.liveChatSponsorshipsHeaderRenderer = new LiveChatSponsorshipsHeaderRenderer();
        }
        public LiveChatSponsorshipsHeaderRenderer liveChatSponsorshipsHeaderRenderer { get; set; }
    }

    public class ContextMenuAccessibility
    {
        public ContextMenuAccessibility()
        {
            this.accessibilityData = new AccessibilityData();
        }

        public AccessibilityData accessibilityData { get; set; }
    }

    public class LiveChatPaidMessageRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatPaidMessageRenderer()
        {
            this.purchaseAmountText = new PurchaseAmountText();
        }

        public PurchaseAmountText purchaseAmountText { get; set; }
        public long headerBackgroundColor { get; set; }
        public long headerTextColor { get; set; }
        public long bodyBackgroundColor { get; set; }
        public long bodyTextColor { get; set; }
        public long authorNameTextColor { get; set; }
        public long timestampColor { get; set; }
    }

    public class LiveChatPaidStickerRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatPaidStickerRenderer()
        {
            this.sticker = new Sticker();
            this.purchaseAmountText = new PurchaseAmountText();
        }

        public Sticker sticker { get; set; }
        public PurchaseAmountText purchaseAmountText { get; set; }
        public long moneyChipBackgroundColor { get; set; }
        public long moneyChipTextColor { get; set; }
        public int stickerDisplayWidth { get; set; }
        public int stickerDisplayHeight { get; set; }
        public long backgroundColor { get; set; }
        public long authorNameTextColor { get; set; }
    }

    public class LiveChatTextMessageRenderer
    {
        public LiveChatTextMessageRenderer()
        {
            this.message = new Message();
            this.authorName = new AuthorName();
            this.authorPhoto = new AuthorPhoto();
            this.authorBadges = new List<AuthorBadge>();
            this.contextMenuEndpoint = new ContextMenuEndpoint();
            this.contextMenuAccessibility = new ContextMenuAccessibility();
        }

        public Message message { get; set; }
        public AuthorName authorName { get; set; }
        public AuthorPhoto authorPhoto { get; set; }
        public ContextMenuEndpoint contextMenuEndpoint { get; set; }
        public string id { get; set; }
        public long timestampUsec { get; set; }
        public string authorExternalChannelId { get; set; }
        public ContextMenuAccessibility contextMenuAccessibility { get; set; }
        public List<AuthorBadge> authorBadges { get; set; }
    }

    public class LiveChatMembershipItemRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatMembershipItemRenderer()
        {
            this.headerPrimaryText = new HeaderPrimaryText();
            this.headerSubtext = new HeaderSubtext();
        }

        public HeaderPrimaryText headerPrimaryText { get; set; }
        public HeaderSubtext headerSubtext { get; set; }
    }

    public class LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer : LiveChatTextMessageRenderer
    {
    }

    public class LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer
    {
        public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer()
        {
            this.header = new Header();
        }

        public string id { get; set; }
        public long timestampUsec { get; set; }
        public string authorExternalChannelId { get; set; }
        public Header header { get; set; }
    }

    public class Item
    {
        public Item()
        {
            this.liveChatTextMessageRenderer = new LiveChatTextMessageRenderer();
            this.liveChatPaidMessageRenderer = new LiveChatPaidMessageRenderer();
            this.liveChatMembershipItemRenderer = new LiveChatMembershipItemRenderer();
            this.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer = new LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer();
            this.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer = new LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer();
            this.liveChatPaidStickerRenderer = new LiveChatPaidStickerRenderer();
        }

        public LiveChatTextMessageRenderer liveChatTextMessageRenderer { get; set; }
        public LiveChatPaidMessageRenderer liveChatPaidMessageRenderer { get; set; }
        public LiveChatMembershipItemRenderer liveChatMembershipItemRenderer { get; set; }
        public LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer liveChatSponsorshipsGiftRedemptionAnnouncementRenderer { get; set; }
        public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer liveChatSponsorshipsGiftPurchaseAnnouncementRenderer { get; set; }
        public LiveChatPaidStickerRenderer liveChatPaidStickerRenderer { get; set; }

        public bool IsPaidMessage
        {
            get
            {
                if (liveChatPaidMessageRenderer.purchaseAmountText.simpleText != null
                    && liveChatPaidMessageRenderer.purchaseAmountText.simpleText != "")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsMembershipItem
        {
            get
            {
                if (liveChatMembershipItemRenderer.headerPrimaryText.runs.Count >= 1)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsNewMembership
        {
            get
            {
                if (liveChatMembershipItemRenderer.headerSubtext.runs.Count >= 1)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsGiftRedemption
        {
            get
            {
                if (liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message.runs.Count >= 1)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsGiftPurchase
        {
            get
            {
                if (liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.runs.Count >= 1)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsPaidSticker
        {
            get
            {
                if (liveChatPaidStickerRenderer.purchaseAmountText.simpleText != null
                    && liveChatPaidStickerRenderer.purchaseAmountText.simpleText != "")
                {
                    return true;
                }
                return false;
            }
        }
    }

    public class ReplacementItem : Item
    {
        public bool IsReplace
        {
            get
            {
                if (liveChatTextMessageRenderer.message.simpleText != null
                    && liveChatTextMessageRenderer.message.simpleText != "")
                {
                    return true;
                }
                return false;
            }
        }
    }

    public class AddChatItemAction
    {
        public AddChatItemAction()
        {
            this.item = new Item();
        }

        public Item item { get; set; }
        public string clientId { get; set; }
    }

    public class ReplaceChatItemAction
    {
        public ReplaceChatItemAction()
        {
            this.replacementItem = new ReplacementItem();
        }

        public ReplacementItem replacementItem { get; set; }
        public string targetItemId { get; set; }
    }

    public class CommentData
    {
        public CommentData()
        {
            this.addChatItemAction = new AddChatItemAction();
            this.replaceChatItemAction = new ReplaceChatItemAction();
        }

        public AddChatItemAction addChatItemAction { get; set; }
        public ReplaceChatItemAction replaceChatItemAction { get; set; }

        public override string ToString()
        {
            String ret = String.Format("{0}:{1}",
                this.addChatItemAction.item.liveChatTextMessageRenderer.authorName.simpleText,
                this.addChatItemAction.item.liveChatTextMessageRenderer.message.simpleText);
            if (this.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText);
            }
            if (this.addChatItemAction.item.liveChatMembershipItemRenderer.headerSubtext.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatMembershipItemRenderer.headerSubtext.simpleText);
            }
            if (this.addChatItemAction.item.liveChatMembershipItemRenderer.headerPrimaryText.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatMembershipItemRenderer.headerPrimaryText.simpleText);
            }
            if (this.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatSponsorshipsGiftRedemptionAnnouncementRenderer.message.simpleText);
            }
            if (this.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer.header.liveChatSponsorshipsHeaderRenderer.primaryText.simpleText);
            }
            if (this.addChatItemAction.item.liveChatPaidStickerRenderer.purchaseAmountText.simpleText != "")
            {
                ret += String.Format(" {0}",
                    this.addChatItemAction.item.liveChatPaidStickerRenderer.purchaseAmountText.simpleText);
            }
            if (this.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorName.simpleText != ""
                && this.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.message.simpleText != "")
            {
                ret += String.Format("{0}:{1}",
                    this.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.authorName.simpleText,
                    this.replaceChatItemAction.replacementItem.liveChatTextMessageRenderer.message.simpleText);
            }
            return ret;
        }
    }
}
