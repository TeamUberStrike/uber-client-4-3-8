
using System.Collections.Generic;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;

public static class EnumerationExtensions
{
    public static T[] ValueArray<S, T>(this Dictionary<S, T> dict)
    {
        T[] array = new T[dict.Count];
        dict.Values.CopyTo(array, 0);
        return array;
    }

    public static S[] KeyArray<S, T>(this Dictionary<S, T> dict)
    {
        S[] array = new S[dict.Count];
        dict.Keys.CopyTo(array, 0);
        return array;
    }

    public static KeyValuePair<S, T> First<S, T>(this Dictionary<S, T> dict)
    {
        var e = dict.GetEnumerator();
        e.MoveNext();
        return e.Current;
    }
}

public static class ColorExtensions
{
    /// <summary>
    /// Sets only the Alpha channel for a color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static Color SetAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Sets the Intensity for the color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static Color SetIntensity(this Color color, float intensity, float alpha)
    {
        return new Color(color.r * intensity, color.g * intensity, color.b * intensity, alpha);
    }
}

public static class VectorExtentions
{
    public static float Width(this Vector2 v)
    {
        return v.x;
    }

    public static float Height(this Vector2 v)
    {
        return v.y;
    }
}

public static class Conversions
{
    public static MysteryBoxShopItem ToUnityItem(this MysteryBoxUnityView mysteryBox)
    {
        //get all items from the shop
        var items = new List<IUnityItem>();
        if (mysteryBox.ExposeItemsToPlayers)
        {
            if (mysteryBox.PointsAttributed > 0)
            {
                items.Add(new PointsUnityItem(mysteryBox.PointsAttributed));
            }
            if (mysteryBox.CreditsAttributed > 0)
            {
                items.Add(new CreditsUnityItem(mysteryBox.CreditsAttributed));
            }

            foreach (var item in mysteryBox.MysteryBoxItems)
            {
                items.Add(ItemManager.Instance.GetItemInShop(item.ItemId));
            }
        }

        return new MysteryBoxShopItem()
        {
            Name = mysteryBox.Name,
            Id = mysteryBox.Id,
            Price = new UberStrike.Core.Models.Views.ItemPrice() { Price = mysteryBox.Price, Currency = mysteryBox.UberStrikeCurrencyType },
            Icon = new DynamicTexture(mysteryBox.IconUrl),
            Image = new DynamicTexture(mysteryBox.ImageUrl),
            View = mysteryBox,
            Items = items,
            Category = mysteryBox.Category,
        };
    }

    public static GUIContent PriceTag(this ItemPrice price, bool printCurrency = false, string tooltip = "")
    {
        switch (price.Currency)
        {
            case UberStrikeCurrencyType.Points:
                return new GUIContent(price.Price.ToString("N0") + (printCurrency ? "Points" : ""), UberstrikeTextures.IconPoints20x20, tooltip);
            case UberStrikeCurrencyType.Credits:
                return new GUIContent(price.Price.ToString("N0") + (printCurrency ? "Credits" : ""), UberstrikeTextures.IconCredits20x20, tooltip);
            default:
                return new GUIContent("N/A");
        }
    }

    public static LuckyDrawShopItem ToUnityItem(this LuckyDrawUnityView luckyDraw)
    {
        var luckyDrawItem = new LuckyDrawShopItem()
        {
            Name = luckyDraw.Name,
            Id = luckyDraw.Id,
            Price = new UberStrike.Core.Models.Views.ItemPrice() { Price = luckyDraw.Price, Currency = luckyDraw.UberStrikeCurrencyType },
            Icon = new DynamicTexture(luckyDraw.IconUrl),
            View = luckyDraw,
            Category = luckyDraw.Category, 
        };

        //get all items from the shop
        var setGroup = new List<LuckyDrawShopItem.LuckyDrawSet>();
        foreach (var s in luckyDraw.LuckyDrawSets)
        {
            var set = new LuckyDrawShopItem.LuckyDrawSet()
            {
                Id = s.Id,
                Items = new List<IUnityItem>(),
                Image = new DynamicTexture(s.ImageUrl),
                View = s,
                Parent = luckyDrawItem,
            };

            if (s.ExposeItemsToPlayers)
            {
                if (s.PointsAttributed > 0)
                {
                    set.Items.Add(new PointsUnityItem(s.PointsAttributed));
                }
                if (s.CreditsAttributed > 0)
                {
                    set.Items.Add(new CreditsUnityItem(s.CreditsAttributed));
                }
                foreach (var item in s.LuckyDrawSetItems)
                {
                    set.Items.Add(ItemManager.Instance.GetItemInShop(item.ItemId));
                }
            }
            setGroup.Add(set);
        }

        luckyDrawItem.Sets = setGroup;
        return luckyDrawItem;
    }
}

public static class RectExtentions
{
    public static Rect FullExtends(this Rect r)
    {
        return new Rect(0, 0, r.width, r.height);
    }

    public static Rect Lerp(this Rect r, Rect target, float time)
    {
        return new Rect(
            Mathf.Lerp(r.x, target.x, time),
            Mathf.Lerp(r.y, target.y, time),
            Mathf.Lerp(r.width, target.width, time),
            Mathf.Lerp(r.height, target.height, time)
            );
    }

    public static Rect ExpandBy(this Rect r, int width, int height)
    {
        return new Rect(r.x, r.y, r.width + width, r.height + height);
    }

    public static Rect OffsetBy(this Rect r, Vector2 offset)
    {
        return new Rect(r.x + offset.x, r.y + offset.y, r.width, r.height);
    }

    public static Rect OffsetBy(this Rect r, float x, float y)
    {
        return new Rect(r.x + x, r.y + y, r.width, r.height);
    }

    public static Rect Add(this Rect r1, Rect r2)
    {
        return new Rect(r1.x + r2.x, r1.y + r2.y, r1.width + r2.width, r1.height + r2.height);
    }

    public static Rect Center(this Rect r)
    {
        return new Rect((Screen.width - r.width) * 0.5f, (Screen.height - r.height) * 0.5f, r.width, r.height);
    }

    public static Rect Center(this Rect r, float width, float height)
    {
        return new Rect((r.width - width) * 0.5f, (r.height - height) * 0.5f, width, height);
    }

    public static Rect CenterHorizontally(this Rect r, float y, float width, float height)
    {
        return new Rect((r.width - width) * 0.5f, y, width, height);
    }

    public static Rect CenterVertically(this Rect r, float x, float width, float height)
    {
        return new Rect(x, (r.height - height) * 0.5f, width, height);
    }
}