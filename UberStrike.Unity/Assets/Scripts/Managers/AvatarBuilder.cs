using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Core.Types;
using UnityEngine;

public class AvatarBuilder : Singleton<AvatarBuilder>
{
    private AvatarBuilder() { }

    private GameObject CreateAvatar(AvatarType type, int head, int face, int gloves, int upper, int lower, int boots)
    {
        return CreateAvatar(PrefabManager.Instance.GetAvatarPrefab(type).gameObject, type != AvatarType.LutzRavinoff, head, face, gloves, upper, lower, boots);
    }

    public GameObject CreateAvatar(GameObject baseAvatar, bool isHolo, int head, int face, int gloves, int upper, int lower, int boots)
    {
        //Note: Gear Item slots may NEVER be null, but Weapon slots can
        GameObject instance = SkinnedMeshCombiner.CreateCharacter(
            isHolo,
            baseAvatar,
            ItemManager.Instance.GetPrefab(head),
            ItemManager.Instance.GetPrefab(face),
            ItemManager.Instance.GetPrefab(gloves),
            ItemManager.Instance.GetPrefab(upper),
            ItemManager.Instance.GetPrefab(lower),
            ItemManager.Instance.GetPrefab(boots)
            );

        if (instance != null && instance.GetComponent<AvatarDecorator>())
        {
            AvatarDecorator avatar = instance.GetComponent<AvatarDecorator>();
            avatar.MyGear = new int[6]
                    {
                        head ,
                        face,
                        gloves,
                        upper,
                        lower,
                        boots,
                    };
        }

        return instance;
    }

    public AvatarDecorator CreateLocalAvatar()
    {
        GameObject instance = CreateAvatar(
            PlayerDataManager.Instance.LocalPlayerAvatarType,
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHead),
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearFace),
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearGloves),
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearUpperBody),
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearLowerBody),
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearBoots));

        AvatarDecorator decorator = instance.GetComponent<AvatarDecorator>();

        if (decorator)
        {
            foreach (LoadoutSlotType slot in LoadoutManager.WeaponSlots)
            {
                InventoryItem item;
                if (LoadoutManager.Instance.TryGetItemInSlot(slot, out item))
                {
                    BaseWeaponDecorator d = ItemManager.Instance.Instantiate(item.Item.ItemId).GetComponent<BaseWeaponDecorator>();
                    d.EnableShootAnimation = false;
                    decorator.AssignWeapon(slot, d);
                }
            }

            //tint the avatar skin
            decorator.UpdateLayers();
            decorator.SetSkinColor(PlayerDataManager.SkinColor);
        }
        else
        {
            CmuneDebug.LogError("No Avatar Prefab found for AvatarType {0}", PlayerDataManager.Instance.LocalPlayerAvatarType);
        }

        decorator.GetComponent<Renderer>().receiveShadows = true;
        decorator.GetComponent<Renderer>().castShadows = true;
        decorator.HudInformation.DistanceCap = 100;
        decorator.HudInformation.SetAvatarLabel(PlayerDataManager.NameSecure);

        return decorator;
    }

    public void UpdateLocalAvatar()
    {
        if (GameState.LocalDecorator != null)
        {
            AvatarType avatarType = PlayerDataManager.Instance.LocalPlayerAvatarType;

            //we use a Holo
            if (GameState.LocalDecorator.Configuration.AvatarType != avatarType)
            {
                AvatarDecorator baseAvatar = PrefabManager.Instance.GetAvatarPrefab(avatarType);

                if (baseAvatar != null)
                {
                    GameObject oldAvatar = GameState.LocalDecorator.gameObject;
                    GameState.LocalDecorator = CreateLocalAvatar();
                    GameState.LocalDecorator.SetPosition(oldAvatar.transform.position, oldAvatar.transform.rotation);
                    Destroy(oldAvatar);
                }
                else
                {
                    CmuneDebug.LogError("No Avatar Prefab found for AvatarType {0}", avatarType);
                }
            }
            else
            {
                var currentLoadout = LoadoutManager.Instance.GetCurrentLoadoutIds();

                UpdateGearItems(
                        GameState.LocalDecorator,
                        avatarType,
                        currentLoadout[LoadoutSlotType.GearHead],
                        currentLoadout[LoadoutSlotType.GearFace],
                        currentLoadout[LoadoutSlotType.GearGloves],
                        currentLoadout[LoadoutSlotType.GearUpperBody],
                        currentLoadout[LoadoutSlotType.GearLowerBody],
                        currentLoadout[LoadoutSlotType.GearBoots]);
            }

            //set the characters skincolor
            GameState.LocalDecorator.SetSkinColor(PlayerDataManager.SkinColor);
            GameState.LocalDecorator.UpdateLayers();

            //Now we can set a default weapon
            GameState.LocalDecorator.ShowWeapon(GameState.LocalDecorator.CurrentWeaponSlot);

            // Set the Avatar name UI
            GameState.LocalDecorator.HudInformation.SetAvatarLabel(PlayerDataManager.IsPlayerInClan ? string.Format("[{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.Name) : PlayerDataManager.Name);
            GameState.LocalDecorator.HudInformation.enabled = true;
            GameState.LocalDecorator.MeshRenderer.enabled = true;
        }
        else
        {
            CmuneDebug.LogError("No local Player created yet! Call 'CreateLocalPlayerAvatar' first!");
        }
    }

    private static Dictionary<UberstrikeItemClass, int> GetLocalPlayerLoadout()
    {
        return new Dictionary<UberstrikeItemClass, int>(6)
        {
            { UberstrikeItemClass.GearHead, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHead) },
            { UberstrikeItemClass.GearFace, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearFace) },
            { UberstrikeItemClass.GearGloves, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearGloves) },
            { UberstrikeItemClass.GearUpperBody, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearUpperBody) },
            { UberstrikeItemClass.GearLowerBody, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearLowerBody) },
            { UberstrikeItemClass.GearBoots, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearBoots) },
            { UberstrikeItemClass.GearHolo, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHolo) },
        };
    }

    public void UpdateLocalAvatarGear(IEnumerable<IUnityItem> items)
    {
        var currentLoadout = GetLocalPlayerLoadout();
        foreach (var item in items)
        {
            if (item != null) currentLoadout[item.ItemClass] = item.ItemId;
        }

        UpdateGearItems(GameState.LocalDecorator,
            GetPlayerAvatarType(currentLoadout[UberstrikeItemClass.GearHolo]),
            currentLoadout[UberstrikeItemClass.GearHead],
            currentLoadout[UberstrikeItemClass.GearFace],
            currentLoadout[UberstrikeItemClass.GearGloves],
            currentLoadout[UberstrikeItemClass.GearUpperBody],
            currentLoadout[UberstrikeItemClass.GearLowerBody],
            currentLoadout[UberstrikeItemClass.GearBoots]);

        //set the characters skincolor
        GameState.LocalDecorator.UpdateLayers();
        GameState.LocalDecorator.SetSkinColor(PlayerDataManager.SkinColor);
    }

    public void UpdateLocalAvatarGear(params IUnityItem[] items)
    {
        UpdateLocalAvatarGear(new List<IUnityItem>(items));
    }

    public AvatarDecoratorConfig CreateRagdoll(AvatarType type, int[] gearItems, Color skinColor)
    {
        GameObject instance = CreateAvatar(
                PrefabManager.Instance.GetRagdollPrefab(type).gameObject,
                type != AvatarType.LutzRavinoff,
                gearItems.Length > 0 ? gearItems[0] : 0,
                gearItems.Length > 1 ? gearItems[1] : 0,
                gearItems.Length > 2 ? gearItems[2] : 0,
                gearItems.Length > 3 ? gearItems[3] : 0,
                gearItems.Length > 4 ? gearItems[4] : 0,
                gearItems.Length > 5 ? gearItems[5] : 0);

        var ragdoll = instance.GetComponent<AvatarDecoratorConfig>();

        ragdoll.SkinColor = skinColor;

        return ragdoll;
    }

    public AvatarDecorator CreateRemoteAvatar(int[] gearItems, Color skinColor)
    {
        GameObject instance = CreateAvatar(
                gearItems.Length > 6 ? AvatarBuilder.GetPlayerAvatarType(gearItems[6]) : AvatarType.LutzRavinoff,
                gearItems.Length > 0 ? gearItems[0] : 0,
                gearItems.Length > 1 ? gearItems[1] : 0,
                gearItems.Length > 2 ? gearItems[2] : 0,
                gearItems.Length > 3 ? gearItems[3] : 0,
                gearItems.Length > 4 ? gearItems[4] : 0,
                gearItems.Length > 5 ? gearItems[5] : 0);

        AvatarDecorator decorator = instance.GetComponent<AvatarDecorator>();

        decorator.SetLayers(UberstrikeLayer.RemotePlayer);
        decorator.SetSkinColor(skinColor);

        //Now we can show the current weapon
        decorator.ShowWeapon(decorator.CurrentWeaponSlot);

        return decorator;
    }

    public void UpdateRemoteAvatar(AvatarDecorator decorator, int[] gearItems, Color skinColor)
    {
        UpdateGearItems(decorator,
                gearItems.Length >= 6 ? AvatarBuilder.GetPlayerAvatarType(gearItems[6]) : AvatarType.LutzRavinoff,
                gearItems[0],
                gearItems[1],
                gearItems[2],
                gearItems[3],
                gearItems[4],
                gearItems[5]);

        decorator.SetLayers(UberstrikeLayer.RemotePlayer);
        decorator.SetSkinColor(skinColor);

        //Now we can show the current weapon
        decorator.ShowWeapon(decorator.CurrentWeaponSlot);
    }

    private void UpdateGearItems(AvatarDecorator instance, AvatarType type, int head, int face, int gloves, int upper, int lower, int boots)
    {
        AvatarDecorator baseAvatar = PrefabManager.Instance.GetAvatarPrefab(type);
        try
        {
            bool isHolo = type != AvatarType.LutzRavinoff;

            //Note: Gear Item slots may NEVER be null, but Weapon slots can
            SkinnedMeshCombiner.UpdateCharacter(
                 isHolo,
                 instance.gameObject,
                 baseAvatar.gameObject,
                 ItemManager.Instance.GetPrefab(head),
                 ItemManager.Instance.GetPrefab(face),
                 ItemManager.Instance.GetPrefab(gloves),
                 ItemManager.Instance.GetPrefab(upper),
                 ItemManager.Instance.GetPrefab(lower),
                 ItemManager.Instance.GetPrefab(boots)
                 );

            //update local gear set - used for ragdoll spawning
            instance.MyGear = new int[6]
                    {
                        head,
                        face,
                        gloves,
                        upper,
                        lower,
                        boots,
                    };

            //update the avatar type, based on the items equipped
            instance.Configuration.AvatarType = type;
        }
        catch
        {
            throw;
        }
    }

    public static AvatarType GetPlayerAvatarType(IUnityItem item)
    {
        if (item is HoloGearItem)
        {
            return ((HoloGearItem)item).Configuration.Holo;
        }
        else
        {
            return AvatarType.LutzRavinoff;
        }
    }

    public static AvatarType GetPlayerAvatarType(int itemId)
    {
        return GetPlayerAvatarType(ItemManager.Instance.GetGearItemInShop(itemId, UberstrikeItemClass.GearHolo));
    }

    public static void Destroy(GameObject obj)
    {
        var renderer = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderer)
        {
            foreach (var m in r.materials)
                GameObject.Destroy(m);
        }
        var skinnedRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderer)
        {
            GameObject.Destroy(skinnedRenderer.sharedMesh);
        }
        GameObject.Destroy(obj);
    }
}