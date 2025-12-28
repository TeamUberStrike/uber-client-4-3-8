
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using Cmune.Util;

public class LuckyDrawShopItem : LotteryShopItem
{
    public LuckyDrawUnityView View { get; set; }
    public List<LuckyDrawSet> Sets { get; set; }

    public class LuckyDrawSet
    {
        public int Id { get; set; }
        public DynamicTexture Image { get; set; }
        public List<IUnityItem> Items { get; set; }
        public LuckyDrawShopItem Parent { get; set; }
        public LuckyDrawSetUnityView View { get; set; }
    }

    public override string Description
    {
        get { return View.Description; }
    }

    public override void Use()
    {
        LotteryManager.Instance.RunLuckyDraw(this);
    }
}