using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using System.Linq;
using System;
using Cmune.Util;

public class LotteryManager : Singleton<LotteryManager>
{
    public const int IMG_WIDTH = 282;
    public const int IMG_HEIGHT = 317;

    private Dictionary<BundleCategoryType, List<LotteryShopItem>> _lotteryItems;
    private List<LuckyDrawShopItem> _luckyDrawItems;

    private LotteryPopupTask2 _task;

    private LotteryManager()
    {
        _luckyDrawItems = new List<LuckyDrawShopItem>();
        _lotteryItems = new Dictionary<BundleCategoryType, List<LotteryShopItem>>();
    }

    private IEnumerable<LotteryShopItem> GetAllItemsOfType(Type type)
    {
        foreach (var list in _lotteryItems.Values)
        {
            foreach (var item in list)
            {
                if (item.GetType() == type)
                    yield return item;
            }
        }
    }

    public void RefreshLotteryItems()
    {
        GetAllLuckyDraws();
        GetAllMysteryBoxes();
    }

    public bool TryGetBundle(BundleCategoryType bundle, out List<LotteryShopItem> items)
    {
        return _lotteryItems.TryGetValue(bundle, out items);
    }

    public void ShowNextItem(LotteryShopItem currentItem)
    {
        var boxes = new List<LotteryShopItem>(GetAllItemsOfType(currentItem.GetType()));
        if (boxes.Count > 0)
        {
            int index = boxes.FindIndex(i => i == currentItem);

            //take a random item if the current item doesn't exist (e.g. daily login)
            if (index < 0)
            {
                boxes[UnityEngine.Random.Range(0, boxes.Count)].Use();
            }
            //otherwise just take the next one in the list
            else
            {
                int next = (index + 1) % boxes.Count;
                boxes[next].Use();
            }
        }
    }

    public void ShowPreviousItem(LotteryShopItem currentItem)
    {
        var boxes = new List<LotteryShopItem>(GetAllItemsOfType(currentItem.GetType()));
        if (boxes.Count > 0)
        {
            int index = boxes.FindIndex(i => i == currentItem);

            //take a random item if the current item doesn't exist (e.g. daily login)
            if (index < 0)
            {
                boxes[UnityEngine.Random.Range(0, boxes.Count)].Use();
            }
            //otherwise just take the next one in the list
            else
            {
                int next = (index - 1 + boxes.Count) % boxes.Count;
                boxes[next].Use();
            }
        }
    }

    public LuckyDrawPopup RunLuckyDraw(LuckyDrawShopItem item)
    {
        var popup = new LuckyDrawPopup(item);
        StartTask(popup);
        return popup;
    }

    public void RunMysteryBox(MysteryBoxShopItem item)
    {
        StartTask(new MysteryBoxPopup(item));
    }

    private void StartTask(LotteryPopupDialog dialog)
    {
        _task = new LotteryPopupTask2(dialog);

        PopupSystem.Show(dialog);
    }

    private void GetAllMysteryBoxes()
    {
        UberStrike.WebService.Unity.ShopWebServiceClient.GetAllMysteryBoxs(
            (list) =>
            {
                foreach (var mysteryBox in list)
                {
                    List<LotteryShopItem> boxes;
                    if (!_lotteryItems.TryGetValue(mysteryBox.Category, out boxes))
                    {
                        boxes = new List<LotteryShopItem>();
                        _lotteryItems[mysteryBox.Category] = boxes;
                    }

                    var unityItem = mysteryBox.ToUnityItem();
                    boxes.Add(unityItem);
                }
            },
            (ex) =>
            {
                Debug.LogError("MysteryBoxManager failed with: " + ex.Message);
            });
    }

    private void GetAllLuckyDraws()
    {
        UberStrike.WebService.Unity.ShopWebServiceClient.GetAllLuckyDraws(
            (list) =>
            {
                foreach (var luckyDraw in list)
                {
                    List<LotteryShopItem> boxes;
                    if (!_lotteryItems.TryGetValue(luckyDraw.Category, out boxes))
                    {
                        boxes = new List<LotteryShopItem>();
                        _lotteryItems[luckyDraw.Category] = boxes;
                    }

                    var unityItem = luckyDraw.ToUnityItem();
                    boxes.Add(unityItem);
                    _luckyDrawItems.Add(unityItem);
                }
            },
            (ex) =>
            {
                Debug.LogError("MysteryBoxManager failed with: " + ex.Message);
            });
    }
}