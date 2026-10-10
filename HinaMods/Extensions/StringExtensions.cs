using System.IO;


namespace Kaguya.HinaMods.Extensions;

// 主要用于获取资源路径的工具方法
public static class StringExtensions
{
    // 拼接通用图片资源路径
    public static string ImagePath(this string path)
    {
        return Path.Join("images", "hinamods", path);
    }

    // 拼接卡牌图片资源路径
    public static string CardImagePath(this string path)
    {
        return $"res://images/hinamods/Cards/{path.Replace('\\', '/')}";
    }

    // 拼接大卡牌图片资源路径
    public static string BigCardImagePath(this string path)
    {
        return $"res://images/hinamods/Cards/Big/{path.Replace('\\', '/')}";
    }

    // 拼接能力图标资源路径
    public static string PowerImagePath(this string path)
    {
        return Path.Join("images", "hinamods", "Powers", path);
    }

    // 拼接大能力图标资源路径
    public static string BigPowerImagePath(this string path)
    {
        return Path.Join("images", "hinamods", "Powers", "Big", path);
    }

    // 拼接遗物图标资源路径
    public static string RelicImagePath(this string path)
    {
        return Path.Join("images", "hinamods", "relics", path);
    }

    // 拼接大遗物图标资源路径
    public static string BigRelicImagePath(this string path)
    {
        return Path.Join("images", "hinamods", "relics", "Big", path);
    }

    // 拼接角色UI资源路径
    public static string CharacterUiPath(this string path)
    {
        return Path.Join("images", "hinamods", "Charui", path);
    }
}
