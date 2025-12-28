using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class PopulateUnityItems : MonoBehaviour
{
    [MenuItem("Cmune Tools/Managers/Populate Unity Item Mall")]
    public static void PopulateItemList()
    {
        if (!Selection.activeGameObject)
        {
            Debug.LogError("Please select a GameObject in the scene!");
            return;
        }

        UnityItemConfiguration itemManager = Selection.activeGameObject.GetComponentInChildren<UnityItemConfiguration>();

        if (itemManager == null)
        {
            Debug.LogError("No 'ItemManager' script attached!");
            return;
        }

        //Gear Items
        itemManager.UnityItemsGear = new List<GearItem>();
        itemManager.UnityItemsGear.AddRange(AddDefaultGearItems());
        itemManager.UnityItemsGear.AddRange(AddGearItems());

        itemManager.UnityItemsHolo = new List<HoloGearItem>();
        itemManager.UnityItemsHolo.AddRange(AddHoloGearItems());

        //Weapon Items
        itemManager.UnityItemsWeapons = new List<WeaponItem>();
        itemManager.UnityItemsWeapons.AddRange(AddDefaultWeaponItems());
        itemManager.UnityItemsWeapons.AddRange(AddWeaponItems());

        //Func Items
        itemManager.UnityItemsFunctional = new List<UnityItemConfiguration.FunctionalItemHolder>();
        itemManager.UnityItemsFunctional.AddRange(AddFunctionalItems());

        Debug.Log("Successfully added all items to ItemManager!");

        CheckValidItems();
    }

    [MenuItem("Cmune Tools/Managers/Validate Unity Item Mall")]
    public static void CheckValidItems()
    {
        UnityItemConfiguration itemManager = Selection.activeGameObject.GetComponentInChildren<UnityItemConfiguration>();

        List<IUnityItem> items = new List<IUnityItem>();

        foreach (var item in itemManager.UnityItemsGear)
        {
            if (item.ItemId > 0)
                items.Add(item);
            else
                Debug.LogError("Gear Prefab is not configured: " + item.Name);
        }
        foreach (var item in itemManager.UnityItemsWeapons)
        {
            if (item.ItemId > 0)
                items.Add(item);
            else
                Debug.LogError("Wepon Prefab is not configured: " + item.Name);
        }
        foreach (var item in itemManager.UnityItemsFunctional)
        {
            if (item.ItemId > 0)
                items.Add(new FunctionalItem()
                    {
                        Configuration = new FunctionalItemConfiguration()
                        {
                            ID = item.ItemId,
                            Name = item.Name,
                        }
                    });
            else
                Debug.LogError("Wepon Prefab is not configured: " + item.Name);
        }

        StringBuilder builder = new StringBuilder();
        //Check for missing items
        foreach (var item in BackendData.SupportedItems)
        {
            if (!items.Exists(i => i.ItemId == item.Key))
            {
                builder.AppendLine("Item was not deprecated but asset is missing: " + item);
            }
        }

        //Check for obsolete items
        foreach (var item in items)
        {
            if (!BackendData.SupportedItems.ContainsKey(item.ItemId))
            {
                builder.AppendLine("Item " + item.Name + " - " + item.ItemId + " is deprecated.");
            }
        }

        if (builder.Length == 0)
            Debug.Log("Item mall is clean");
        else
            Debug.LogError(builder.ToString());
    }

    private static void GetWeaponItem(string path, List<WeaponItem> list)
    {
        var go = GetPrefab(path);
        if (go)
        {
            var component = go.GetComponent<WeaponItem>();
            if (component) list.Add(component);
            else Debug.LogError("GetWeaponItem cant find component of type WeaponItem: " + path);
        }
        else
        {
            Debug.LogError("WeaponItem, not prefab found at: " + path);
        }
    }

    private static void GetGearItem(string path, List<GearItem> list)
    {
        var go = GetPrefab(path);
        if (go)
        {
            var component = go.GetComponent<GearItem>();
            if (component) list.Add(component);
            else Debug.LogError("GetWeaponItem cant find component of type GearItem: " + path);
        }
        else
        {
            Debug.LogError("GetGearItem, not prefab found at: " + path);
        }
    }

    private static void GetHoloItem(string path, List<HoloGearItem> list)
    {
        var go = GetPrefab(path);
        if (go)
        {
            var component = go.GetComponent<HoloGearItem>();
            if (component) list.Add(component);
            else Debug.LogError("GetWeaponItem cant find component of type HoloGearItem: " + path);
        }
        else
        {
            Debug.LogError("HoloGearItem, not prefab found at: " + path);
        }
    }

    //Lutz Ravinoff
    public static List<GearItem> AddDefaultGearItems()
    {
        var unityGearItemList = new List<GearItem>();
        GetGearItem("Characters/LutzRavinoff/Base/Prefabs/LutzDefaultGearHead.prefab", unityGearItemList);
        GetGearItem("Characters/LutzRavinoff/Base/Prefabs/LutzDefaultGearGloves.prefab", unityGearItemList);
        GetGearItem("Characters/LutzRavinoff/Base/Prefabs/LutzDefaultGearUpperBody.prefab", unityGearItemList);
        GetGearItem("Characters/LutzRavinoff/Base/Prefabs/LutzDefaultGearLowerBody.prefab", unityGearItemList);
        GetGearItem("Characters/LutzRavinoff/Base/Prefabs/LutzDefaultGearBoots.prefab", unityGearItemList);
        return unityGearItemList;
    }

    public static List<WeaponItem> AddDefaultWeaponItems()
    {
        var unityWeaponItemList = new List<WeaponItem>();
        GetWeaponItem("Weapons/Melee/TheSplatbat/Prefabs/TheSplatbat.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/HG/Default/Prefabs/HandGun.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/MG/Default/Prefabs/MachineGun.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/SG/PaintShotty/Prefabs/PaintShotty.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/SR/Default/Prefabs/PaintSniper.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/CN/Default/Prefabs/Cannon.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/SP/Default/Prefabs/SplatterGun.prefab", unityWeaponItemList);
        GetWeaponItem("Weapons/LR/Default/Prefabs/Launcher.prefab", unityWeaponItemList);
        return unityWeaponItemList;
    }

    public static List<WeaponItem> AddWeaponItems()
    {
        var unityItemList = new List<WeaponItem>();

        // CN
        GetWeaponItem("Weapons/CN/ForceCannon/Prefabs/ForceCannon.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/Paintzerfaust/Prefabs/Paintzerfaust.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/EnigmaCannon/Prefabs/EnigmaCannon.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/ForceCannonPlus/Prefabs/ForceCannonPlus.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/UltimaCannon/Prefabs/UltimaCannon.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/EnigmaCannon_Valentine2012/EnigmaCannon_Valentine.prefab", unityItemList);

        // HG
        GetWeaponItem("Weapons/HG/Executioner/Prefabs/Executioner.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/Judge/Prefabs/Judge.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/Jury/Prefabs/Jury.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/GodFather/Prefabs/GoldPistol.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/SpitefulSkewer/Prefabs/SpitefulSkewer.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_BaseNoSilencer.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_CamoNoSilencer.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_BlackNoSilencer.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_PimpNoSilencer.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_Base.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_Black.prefab", unityItemList);
        GetWeaponItem("Weapons/HG/USP/Prefabs/USP_Pimp.prefab", unityItemList);

        // LR
        GetWeaponItem("Weapons/LR/Enamelator/Prefabs/Enamelator.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/MortarExporter/Prefabs/MortarExporter.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/FinalWord/Prefabs/FinalWord.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/iLauncher/Prefabs/iLauncher.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/Demolisher/Prefabs/Demolisher.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/HaloCannon/HaloCannon.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/HaloCannon/HaloCannon_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/LR/HaloCannon/HaloCannon_Veteran.prefab", unityItemList);

        // MG
        GetWeaponItem("Weapons/MG/BattleSnake/Prefabs/BattleSnake.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/ShadowGun/Prefabs/ShadowGun.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UMG/Prefabs/UMG.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/Sabertooth/Prefabs/Sabertooth.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UZI/Prefabs/UZI_Base.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UZI/Prefabs/UZI_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UZI/Prefabs/UZI_Pimp.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UZI/Prefabs/UZI_Gold.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/UZI/Prefabs/UZI_Tiger.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AssaultRifle/Prefabs/AssaultRifle_Brown.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AssaultRifle/Prefabs/AssaultRifle_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AssaultRifle/Prefabs/AssaultRifle_Pimp.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AssaultRifle/Prefabs/AssaultRifle_Blue.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AK47/AK47.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AK47/AK47_Black.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AK47/AK47_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/AK47/AK47_Tiger.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Black.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Black_S.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Camo_S.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Standard.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Standard_S.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Tiger.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/M4/M4_Tiger_S.prefab", unityItemList);

        // SG
        GetWeaponItem("Weapons/SG/PainHammer/PainHammer.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/DeathHammer/DeathHammer.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/OblivionHammer/OblivionHammer.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/SnotGun/Prefabs/SnotGun.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/Thunderbuss/Prefabs/ThunderbussBlue.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/Firewave/Prefabs/Firewave.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/AutomaticShotGun/Prefabs/Automatic_Shotgun_Roughed.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/AutomaticShotGun/Prefabs/Automatic_Shotgun_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/AutomaticShotGun/Prefabs/Automatic_Shotgun_Pimp.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/AutomaticShotGun/Prefabs/Automatic_Shotgun_Black.prefab", unityItemList);

        // SP
        GetWeaponItem("Weapons/SP/MadSplatter/Prefabs/MadSplatter.prefab", unityItemList);
        GetWeaponItem("Weapons/SP/MagmaRifle/Prefabs/MagmaRifle.prefab", unityItemList);
        GetWeaponItem("Weapons/SP/Vandalizer/Prefabs/Vandalizer.prefab", unityItemList);
        GetWeaponItem("Weapons/SP/ArcticRifle/Prefabs/ArcticRifle.prefab", unityItemList);

        // SR
        GetWeaponItem("Weapons/SR/Deliverator/Prefabs/Deliverator.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/OrdinatorRifle/Prefabs/OrdinatorRifle.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/ParticleLance/Prefabs/ParticleLance.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/Vanquisher/Prefabs/Vanquisher.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/NefariousNeedler/Prefabs/NefariousNeedler.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/FusionLance/Prefabs/FusionLance.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/DarkVanquisher/Prefabs/DarkVanquisher.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/Vanquisher_Kongregate/Vanquisher_Kongregate.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/Vlad/Prefabs/Vlad.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/Snapshot/Prefabs/Snapshot.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/AWP/Prefabs/AWP_Roughed.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/AWP/Prefabs/AWP_Black.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/AWP/Prefabs/AWP_Camo.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/AWP/Prefabs/AWP_Pimp.prefab", unityItemList);

        //MELEE
        GetWeaponItem("Weapons/Melee/Prefabs/Kantana.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/Sickle.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/DundeeKnifeFPS.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/RamboKnifeFPS.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/PirateKnifeFPS.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/Hammer.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/SantaScythe.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/ZombieAxe.prefab", unityItemList);


        //Dragon Edition
        GetWeaponItem("Weapons/MG/UMG_NewYearEdition/UMG_NewYearEdition.prefab", unityItemList);
        GetWeaponItem("Weapons/SP/MagmaRifle_NewYearEdition/MagmaRifle_NewYearEdition.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/Vanquisher_NewYearEdition/Vanquisher_NewYearEdition.prefab", unityItemList);
        GetWeaponItem("Weapons/MG/BattleSnake/Prefabs/BattleSnake-DE.prefab", unityItemList);
        GetWeaponItem("Weapons/Melee/Prefabs/MythicEdge-DE.prefab", unityItemList);
        GetWeaponItem("Weapons/SG/Thunderbuss/Prefabs/Thunderbuss-DE.prefab", unityItemList);
        GetWeaponItem("Weapons/CN/EnigmaCannon-DragonEdition/EnigmaCannon-DE.prefab", unityItemList);
        GetWeaponItem("Weapons/SR/ParticleLance-DragonEdition/ParticleLance-DE.prefab", unityItemList);

        return unityItemList;
    }

    public static string GetName(int itemId)
    {
        string name;
        if (!BackendData.SupportedItems.TryGetValue(itemId, out name))
        {
            Debug.LogError("Item not supported by backend: " + itemId);
            name = "### UNSUPPORTED ### " + itemId;
        }

        return name;
    }

    public static List<HoloGearItem> AddHoloGearItems()
    {
        var unityItemList = new List<HoloGearItem>();

        //Holo
        GetHoloItem("Characters/LutzRavinoff/Gear/Holo/CyborgZombieHolo.prefab", unityItemList);
        GetHoloItem("Characters/LutzRavinoff/Gear/Holo/HumanZombieHolo.prefab", unityItemList);
        GetHoloItem("Characters/LutzRavinoff/Gear/Holo/JuliaHolo.prefab", unityItemList);
        GetHoloItem("Characters/LutzRavinoff/Gear/Holo/JuliaNinjaHolo.prefab", unityItemList);

        return unityItemList;
    }

    public static List<GearItem> AddGearItems()
    {
        var unityItemList = new List<GearItem>();

        //Head
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/GreenBeret.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/DundeeHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/NinjaHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/NinjaHeadBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/NinjaHeadWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/PirateHead1.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/PirateHead2.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/RamboHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/PumpkinHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/BoyHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/GirlHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_1_Golden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_2_Golden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_3_Golden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_1_Black.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_2_Black.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KnightHead_3_Black.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/SantaHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Tron_Head_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Tron_Head_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Tron_Head_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/WhiteSkull.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/ArcticDreadsHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/MegatronHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/JuggernautHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/BlackCorpsHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/JuggernautHeadDE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/KongregateHead.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Vampire_Hair.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Halo_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/CamoHalo_L_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Halo_M_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/CamoHalo_M_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Veteran_Halo_L_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Veteran_Halo_M_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Halo_Heavy_Head.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Halo_Heavy_Head_Camo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Halo_Heavy_Head_Veteran.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/MaskSkull_Military.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Blood.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_BlueTooth.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Camouflage.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Comic_Smile.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Comic_Splat.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Gold.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Military_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Military_Green.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Military_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_RedStar.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Simple_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Simple_Green.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Simple_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_US.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/Skull_YellowTooth.prefab", unityItemList);

        //Face
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/Sunglasses.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/BeardAndMo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/NinjaMaskBlack.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/NinjaMaskTeeth.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/NinjaFaceBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/NinjaFaceWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/PirateEyepatch.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/HockeyMask.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Australia.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Brazil.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Canada.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_China.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Europe.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_France.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Germany.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Holand.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_HongKong.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_India.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Italy.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Japan.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Latvia.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Malaysia.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Mexico.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_NewZealand.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Philipines.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Poland.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Portugal.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Romania.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Russia.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Singapore.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Korea.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Spain.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Turkey.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_UK.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_USA.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/FlagMask_Taiwan.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/ArcticDreadsFace.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/BlackCorpsFace.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/SantaBeard.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/Vampire_Face.prefab", unityItemList);

        //Gloves
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/NinjaGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/NinjaGlovesBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/NinjaGlovesWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/PirateGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/RamboGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/SkeletonGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/KnightGlovesGolden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/KnightGlovesBlack.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Tron_Gloves_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Tron_Gloves_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Tron_Gloves_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/ArcticDreadsGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/MegatronGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/JuggernautGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/BlackCorpsGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/SantaGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/JuggernautGlovesDE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/KongregateGloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Vampire_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Halo_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/CamoHalo_L_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Halo_M_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/CamoHalo_M_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Veteran_Halo_L_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Veteran_Halo_M_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Halo_Heavy_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Halo_Heavy_Gloves_Camo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Halo_Heavy_Gloves_Veteran.prefab", unityItemList);

        //Upper Body
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/DundeeUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/NinjaUpperBodyBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/NinjaUpperBodyWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/NinjaUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/PirateUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/RamboUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Admin.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/BetaHero.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/DefaultShirt.prefab", unityItemList); ;
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/SkeletonUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/MafiaUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/LunaticLabjacket.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/LeatherCoat.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Godfather.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/ClassyCoat.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/007.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/UberJacket.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/KnightUpperbodyGolden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/KnightUpperbodyBlack.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Tron_Upperbody_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Tron_Upperbody_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Tron_Upperbody_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/ArcticDreadsUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/GreenTShirt.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/GlobalModeratorTShirt.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/ModeratorTShirt.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/MegatronUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/JuggernautUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/BlackCorpsUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Upperbody/Prefabs/SantaUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/JuggernautUpperbodyDE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/KongregateUpperbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/KongregateJacketBasic.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/KongregateJacketAdvanced.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Vampire_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Halo_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CamoHalo_L_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Halo_M_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CamoHalo_M_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/QAShirt.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Veteran_Halo_L_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Veteran_Halo_M_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Halo_Heavy_UB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Halo_Heavy_UB_Camo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Halo_Heavy_UB_Veteran.prefab", unityItemList);

        //Lower Body
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/ProtectivePants.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/DundeeLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/NinjaLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/NinjaLowerBodyBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/NinjaLowerBodyWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/PirateLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/RamboLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/SkeletonLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/MafiaLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/BlackPants.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/KnightLowerbodyGolden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/KnightLowerbodyBlack.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Tron_Lowerbody_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Tron_Lowerbody_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Tron_Lowerbody_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/ArcticDreadsLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/MegatronLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/JuggernautLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/BlackCorpsLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Lowerbody/Prefabs/SantaLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/JuggernautLowerbodyDE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/KongregateLowerbody.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Vampire_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Halo_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/CamoHalo_L_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Halo_M_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/CamoHalo_M_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Veteran_Halo_L_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Veteran_Halo_M_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Halo_Heavy_LB.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Halo_Heavy_LB_Camo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Halo_Heavy_LB_Veteran.prefab", unityItemList);

        //Boots
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/NinjaShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/NinjaBootsBlack-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/NinjaBootsWhite-DE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/PirateShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/RamboShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/DundeeShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/SkeletonShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/LeatherShoes.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/KnightBootsGolden.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/KnightBootsBlack.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Tron_Boots_Blue.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Tron_Boots_Red.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Tron_Boots_Yellow.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/ArcticDreadsBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/MegatronBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/JuggernautBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/BlackCorpsBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/SantaBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/JuggernautBootsDE.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/KongregateBoots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Vampire_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Halo_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/CamoHalo_L_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Halo_M_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/CamoHalo_M_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Veteran_Halo_L_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Veteran_Halo_M_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Halo_Heavy_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Halo_Heavy_Boots_Camo.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Halo_Heavy_Boots_Veteran.prefab", unityItemList);

        //CT
        GetGearItem("Characters/LutzRavinoff/Gear/Head/Prefabs/CounterTerrorist_Hat.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/CounterTerrorist_Face.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/CounterTerrorist_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/CounterTerrorist_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CounterTerrorist_UB_Germany.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CounterTerrorist_UB_France.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CounterTerrorist_UB_England.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CounterTerrorist_UB_Usa.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/CounterTerrorist_UB_Russia.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/CounterTerrorist_LB.prefab", unityItemList);

        GetGearItem("Characters/LutzRavinoff/Gear/Face/Prefabs/Terrorist_Face.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Gloves/Prefabs/Terrorist_Gloves.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/Boots/Prefabs/Terrorist_Boots.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/UpperBody/Prefabs/Terrorist_UB_Default.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Terrorist_LB_Default.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Terrorist_LB_Camo01.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Terrorist_LB_Camo02.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Terrorist_LB_Camo03.prefab", unityItemList);
        GetGearItem("Characters/LutzRavinoff/Gear/LowerBody/Prefabs/Terrorist_LB_Camo04.prefab", unityItemList);

        return unityItemList;
    }

    public static List<UnityItemConfiguration.FunctionalItemHolder> AddFunctionalItems()
    {
        var unityFunctionalItemList = new List<UnityItemConfiguration.FunctionalItemHolder>();

        unityFunctionalItemList.Add(new UnityItemConfiguration.FunctionalItemHolder()
        {
            ItemId = 1234,
            Name = GetName(1234),
            Icon = GetTexture("Icons/ClanLicense48x48.psd")
        });
        unityFunctionalItemList.Add(new UnityItemConfiguration.FunctionalItemHolder()
        {
            ItemId = 1094,
            Name = GetName(1094),
            Icon = GetTexture("Icons/PrivateersLisense.psd")
        });
        unityFunctionalItemList.Add(new UnityItemConfiguration.FunctionalItemHolder()
        {
            ItemId = 1265,
            Name = GetName(1265),
            Icon = GetTexture("Icons/MacLicense.psd")
        });
        unityFunctionalItemList.Add(new UnityItemConfiguration.FunctionalItemHolder()
        {
            ItemId = 1294,
            Name = GetName(1294),
            Icon = GetTexture("ItemIcons/FunctionalItems/NameChange48x48.psd")
        });

        return unityFunctionalItemList;
    }

    private static Texture2D GetTexture(string fileName)
    {
        if (AssetDatabase.LoadAssetAtPath("Assets/Artwork/GUI/ItemIcons/" + fileName, typeof(Texture2D)) != null)
            return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Artwork/GUI/ItemIcons/" + fileName, typeof(Texture2D));
        else
        {
            if (AssetDatabase.LoadAssetAtPath("Assets/Artwork/GUI/" + fileName, typeof(Texture2D)) != null)
            {
                return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Artwork/GUI/" + fileName, typeof(Texture2D));
            }
            else
            {
                Debug.LogError(string.Format("{0} Texture Not Found!", fileName));
                return null;
            }
        }
    }

    private static BaseWeaponDecorator GetWeaponPrefab(string fileName)
    {
        var weapon = GetPrefab(fileName);

        if (weapon != null) return weapon.GetComponent<BaseWeaponDecorator>();
        else return null;
    }

    private static GameObject GetPrefab(string fileName)
    {
        if (AssetDatabase.LoadAssetAtPath("Assets/Artwork/" + fileName, typeof(GameObject)) != null)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath("Assets/Artwork/" + fileName, typeof(GameObject)) as GameObject;
            return go;
        }
        else
        {
            Debug.LogError(string.Format("{0} Prefab Not Found!", fileName));
            return null;
        }
    }

    private static void TurnCastAndReceiveShadwosOff(GameObject go)
    {
        if (go)
        {
            Renderer[] tempMR = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in tempMR)
            {
                r.castShadows = false;
                r.receiveShadows = false;
            }
        }
    }
}
