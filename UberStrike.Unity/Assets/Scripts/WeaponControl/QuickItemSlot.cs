//using UberStrike.DataCenter.Common.Entities;
//using UnityEngine;

//public class QuickItemSlot
//{
//    public QuickItemSlot(LoadoutSlotType slot, QuickItem item)
//    {
//        Slot = slot;
//        Item = item;

//        if (item != null && item.ItemClass == UberstrikeItemClass.QuickUseGrenade)
//        {
//            Logic = new QuickItemWeapon(item.Configuration);
//        }

//        //take all the config values and apply to the LOGIC and the DECORATOR
//    }

//    public LoadoutSlotType Slot;
//    public BaseQuickItemLogic Logic;
//    public QuickItem Item;
//}