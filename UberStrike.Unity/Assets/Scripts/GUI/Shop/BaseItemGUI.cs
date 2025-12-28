using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public abstract class BaseItemGUI
{
    private int _armorPoints;
    private string _description = "No description available.";
    private BuyingLocationType _location;
    private BuyingRecommendationType _recommendation;

    public IUnityItem Item { get; private set; }

    public IBaseItemDetailGUI DetailGUI { get; private set; }

    public BaseItemGUI(IUnityItem item, BuyingLocationType location, BuyingRecommendationType recommendation)
    {
        _location = location;
        _recommendation = recommendation;

        if (item == null)
        {
            item = new GearItem()
            {
                Icon = UberstrikeIcons.White,
                Configuration = new GearItemConfiguration(),
            };
        }

        Item = item;

        if (Item.ItemType == UberstrikeItemType.Weapon)
        {
            DetailGUI = new WeaponItemDetailGUI(item as WeaponItem);
        }
        else if (Item.ItemClass == UberstrikeItemClass.GearUpperBody || Item.ItemClass == UberstrikeItemClass.GearLowerBody)
        {
            _armorPoints = ((GearItem)item).Configuration.ArmorPoints;
            DetailGUI = new ArmorItemDetailGUI(item as GearItem, UberstrikeIcons.ItemArmorPoints);
        }

        if (Item.ItemView != null && !string.IsNullOrEmpty(Item.ItemView.Description))
        {
            _description = Item.ItemView.Description;
        }
    }

    public abstract void Draw(Rect rect, bool selected);

    public void DrawIcon(Rect rect)
    {
        GUI.Label(rect, Item.Icon, BlueStonez.item_slot_small);
    }

    public void DrawName(Rect rect)
    {
        if (!string.IsNullOrEmpty(Item.Name))
            GUI.Label(rect, Item.Name, BlueStonez.label_interparkbold_11pt_left_wrap);
    }

    public void DrawHintArrow(Rect rect)
    {
        if (rect.Contains(Event.current.mousePosition))
        {
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Label(new Rect((rect.width) / 2f - 16, rect.yMin, UberstrikeIcons.ExpandBigArrow.width, UberstrikeIcons.ExpandBigArrow.height), UberstrikeIcons.ExpandBigArrow, GUIStyle.none);
            GUI.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public void DrawArmorOverlay()
    {
        if (Item.ItemClass == UberstrikeItemClass.GearUpperBody ||
            Item.ItemClass == UberstrikeItemClass.GearLowerBody)
        {
            if (_armorPoints > 00) GUI.DrawTexture(new Rect(04, 35, 16, 16), UberstrikeIcons.ItemArmorPoints);
            if (_armorPoints > 15) GUI.DrawTexture(new Rect(08, 35, 16, 16), UberstrikeIcons.ItemArmorPoints);
            if (_armorPoints > 30) GUI.DrawTexture(new Rect(12, 35, 16, 16), UberstrikeIcons.ItemArmorPoints);
            if (_armorPoints > 45) GUI.DrawTexture(new Rect(16, 35, 16, 16), UberstrikeIcons.ItemArmorPoints);
        }
    }

    public void DrawPromotionalTag()
    {
        if (Item.ItemView != null)
        {
            switch (Item.ItemView.ShopHighlightType)
            {
                case ItemShopHighlightType.New:
                    GUI.DrawTexture(new Rect(0, -3, 32, 32), UberstrikeIcons.PromotionNew); break;
                case ItemShopHighlightType.Featured:
                    GUI.DrawTexture(new Rect(0, -3, 32, 32), UberstrikeIcons.PromotionSale); break;
                case ItemShopHighlightType.Popular:
                    GUI.DrawTexture(new Rect(0, -3, 32, 32), UberstrikeIcons.PromotionHot); break;
            }
        }
    }

    public void DrawClassIcon()
    {
        GUI.color = new Color(1f, 1f, 1f, 0.5f);

        if (Item.ItemType == UberstrikeItemType.Weapon || Item.ItemType == UberstrikeItemType.Gear)
        {
            GUI.DrawTexture(new Rect(54, 4, 24, 24), UberstrikeIcons.GetIconForItemClass(Item.ItemClass));
        }
    }

    public void DrawLevelRequirement()
    {
        if (Item.ItemView != null)
        {
            GUI.color = Item.ItemView.LevelLock > 1 ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(new Rect(54, 29, 24, 24), UberstrikeIcons.LevelLock);

            if (Item.ItemView.LevelLock > 1)
            {
                GUI.Label(new Rect(54, 33, 24, 24), Item.ItemView.LevelLock.ToString(), BlueStonez.label_interparkbold_11pt);
            }
            GUI.color = Color.white;
        }
    }

    protected void DrawPrice(Rect rect, ItemPrice points, ItemPrice credits)
    {
        float x = 0;
        if (points != null)
        {
            string price = string.Format("{0}", points.Price == 0 ? "FREE" : points.Price.ToString("N0"));
            GUI.DrawTexture(new Rect(rect.x, rect.y, 16, 16), ShopUtils.CurrencyIcon(points.Currency));
            GUI.Label(new Rect(rect.x + 20, rect.y + 3, rect.width - 20, 16), price, BlueStonez.label_interparkmed_11pt_left);
            x += 40 + BlueStonez.label_interparkmed_11pt_left.CalcSize(new GUIContent(price)).x;
        }

        if (credits != null)
        {
            string price = string.Format("{0}", credits.Price == 0 ? "FREE" : credits.Price.ToString("N0"));

            if (x > 0)
            {
                GUI.Label(new Rect(rect.x + x - 10, rect.y + 3, 10, 16), "/", BlueStonez.label_interparkmed_11pt_left);
            }

            GUI.DrawTexture(new Rect(rect.x + x, rect.y, 16, 16), ShopUtils.CurrencyIcon(credits.Currency));
            GUI.Label(new Rect(rect.x + x + 20, rect.y + 3, rect.width - 20, 16), price, BlueStonez.label_interparkmed_11pt_left);
        }
    }

    protected void DrawRecommendPrice(Rect rect, ItemPrice price)
    {
        if (price != null)
        {
            string value = price.Price.ToString("N0");
            Vector2 size = BlueStonez.label_interparkmed_11pt_left.CalcSize(new GUIContent(value));

            GUI.Label(new Rect(rect.width - size.x - 127, rect.y + 1, 100, 16), "FROM", BlueStonez.label_interparkbold_11pt_right);
            GUI.DrawTexture(new Rect(rect.width - size.x - 20, rect.y + 1, 16, 16), ShopUtils.CurrencyIcon(price.Currency));
            GUI.Label(new Rect(rect.width - size.x, rect.y + 1, size.x, 16), value, BlueStonez.label_interparkmed_11pt_right);
        }
    }

    public void DrawEquipButton(Rect rect, string content)
    {
        if ((Item.ItemType == UberstrikeItemType.Weapon || Item.ItemType == UberstrikeItemType.Gear || Item.ItemType == UberstrikeItemType.QuickUse)
            && GUI.Button(rect, new GUIContent(content), BlueStonez.buttondark_medium))
        {
            if (Item != null)
            {
                switch (Item.ItemType)
                {
                    case UberstrikeItemType.Gear:
                        CmuneEventHandler.Route(new SelectLoadoutAreaEvent() { Area = LoadoutArea.Gear });
                        break;
                    case UberstrikeItemType.Weapon:
                        CmuneEventHandler.Route(new SelectLoadoutAreaEvent() { Area = LoadoutArea.Weapons });
                        break;
                    case UberstrikeItemType.QuickUse:
                        CmuneEventHandler.Route(new SelectLoadoutAreaEvent() { Area = LoadoutArea.QuickItems });
                        break;
                }

                if (InventoryManager.Instance.EquipItem(Item.ItemId))
                {
                    CmuneEventHandler.Route(new UpdateRecommendationEvent());
                }
                else
                {
                    BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
                    if (panel)
                    {
                        panel.SetItem(Item, _location, _recommendation);
                    }
                }
            }
        }
    }

    public void DrawTryButton(Rect position)
    {
        if (GUI.Button(position, new GUIContent(LocalizedStrings.Try), BlueStonez.buttondark_medium))
        {
            CmuneEventHandler.Route(new ShopTryEvent() { Item = this.Item });
        }
    }

    public void DrawBuyButton(Rect position, string text, ShopArea area = ShopArea.Shop)
    {
        GUI.contentColor = ColorScheme.UberStrikeYellow;
        if (GUITools.Button(position, new GUIContent(text), BlueStonez.buttondark_medium))
        {
            //BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
            //if (panel) panel.SetupBuyPanel(Item, ShopArea.Shop, _location, _recommendation);

            BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
            if (panel)
            {
                panel.SetItem(Item, _location, _recommendation);
            }
        }

        GUI.contentColor = Color.white;
    }

    public void DrawGrayLine(Rect position)
    {
        GUI.Label(new Rect(4, position.height - 1, position.width - 4, 1), string.Empty, BlueStonez.horizontal_line_grey95);
    }

    public void DrawDescription(Rect position)
    {
        GUI.Label(position, _description, BlueStonez.label_itemdescription);
    }

    public void DrawUseButton(Rect position)
    {
        if (GUITools.Button(position, new GUIContent("Use"), BlueStonez.buttondark_medium))
        {
            PanelManager.Instance.OpenPanel(PanelType.NameChange);
        }
    }
}