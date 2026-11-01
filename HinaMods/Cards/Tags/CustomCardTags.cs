namespace Kaguya.HinaMods
{
/// <summary>
/// 自定义卡牌标签常量。
///   ECHO（"Echo"）—— 卡面显示为【歌声】：打出后进消耗堆，下回合抽牌前洗回抽牌堆。
///   SUPPORT（"Support"）—— 支援。
///   SUPPORTCARD（"SupportCard"）—— 支援牌。
/// 基础设施类，无本地化键（【歌声】的文本键在 card_keywords.json 的 KAGUYA-ECHO.*）。
/// </summary>
    public static class CustomCardTags
    {
        public const string ECHO = "Echo";         // 回音标签（打出进消耗堆，下回合抽牌前洗回抽牌堆）
        public const string SUPPORT = "Support";   // 支援标签
        public const string SUPPORTCARD = "SupportCard";   // 支援牌标签
    }
}
