using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Kaguya.HinaMods
{
    /// <summary>
    /// 本 mod 注册的自定义卡牌关键词。
    /// <para>
    /// [CustomEnum] 让 BaseLib 在 ModelDb.Init 时生成一个唯一的 CardKeyword 值并写回本字段；
    /// [KeywordProperties(AutoKeywordPosition.After)] 让它与原生「消耗 / 永恒」走同一条渲染路径 ——
    /// 拥有该关键词的卡牌会在描述**末尾**自动追加一行标题（CardKeywordOrder.afterDescription）。
    /// </para>
    /// <para>
    /// 本地化键由「命名空间首段大写 + '-' + 字段名大写」拼成（Kaguya → "KAGUYA-"），
    /// 因此需要在 card_keywords 表中提供 KAGUYA-ECHO.title / KAGUYA-ECHO.description。
    /// </para>
    /// <para>
    /// ⚠ 字段值由 BaseLib 在运行时写入，不要在任何静态初始化 / 常量上下文中提前捕获
    /// （那会读到 0 = None）。CanonicalKeywords 是运行时求值的实例属性，可安全引用。
    /// </para>
    /// </summary>
    public static class KaguyaKeywords
    {
        [CustomEnum(null)]
        [KeywordProperties(AutoKeywordPosition.After)]
        public static CardKeyword Echo;
    }
}
