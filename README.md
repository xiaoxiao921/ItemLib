# ItemLib
A library for custom items, equipments, buffs and elites in Risk of Rain 2.

### Table of Contents

- [Installation](https://github.com/xiaoxiao921/ItemLib#usage)
- [Project Setup](https://github.com/xiaoxiao921/ItemLib#project-setup)

## Installation

ItemLib is a BepinEx plugin and a Monomod patch, that will also be used as an assembly reference for your BepinEx Plugin (and an Unity AssetBundle for custom assets)

### Installing ItemLib

- [Install BepInEx](https://thunderstore.io/package/bbepis/BepInExPack)
- Download the latest [release](https://thunderstore.io/package/xiaoxiao921/ItemLib/)
- Follow the instructions there.

## Project Setup

### Building from source

- Clone / Download as zip the repo
- Open `ItemLib.sln`

You may have to fix the assembly reference `ItemLib.dll` for the ExampleItemMod project.

### Using the library for your mod

Once your project is ready you'll want to have a method defined that will return a CustomItem / CustomEquipment / CustomBuff / CustomElite object and have an Item Attribute at the top so that the library can load it. \
Depending on what you want, you'll want to change the ItemType (enum) in the attribute. \
Leave both `pickupModelPath` and/or `pickupIconPath` empty if you want to have a custom prefab / icon for your item. \
For having a custom prefab and icon you will need to make an AssetBundle in Unity, you could also download the unitypackage and use it as an example.

A class example for a BepinEx mod is available [here](https://github.com/xiaoxiao921/ItemLib/blob/master/ExampleItemMod/ExampleItemMod.cs)

An AssetBundle project example is available [here](https://github.com/xiaoxiao921/ItemLib/raw/master/AssetBundle%20Example%20Project.unitypackage)


#### Custom Item Method Example
```csharp
[Item(ItemAttribute.ItemType.Item)]
public static CustomItem Test()
{
	// Load the AssetBundle you made with the Unity Editor

	_exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Rampage_data");

	_prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
	_icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

	ItemDef newItemDef = new ItemDef
	{
		tier = ItemTier.Tier3,
		pickupModelPath = "", // leave it empty and give directly the prefab / icon on the return but you can also use an already made prefab by putting a path in there.
		pickupIconPath = "",
		nameToken = "Custom Item Example",
		pickupToken = "i'm a custom item. i do sticky bomb on kill",
		descriptionToken = "yes"
	};

	_itemDisplayRules = new ItemDisplayRule[1]; // keep this null if you don't want the item to show up on the survivor 3d model. You can have multiple rules !
	_itemDisplayRules[0].followerPrefab = _prefab; // the prefab that will show up on the survivor
	_itemDisplayRules[0].childName = "Chest"; // this will define the starting point for the prefab, you can see what are the differents name available in the prefab model of the survivors.
	_itemDisplayRules[0].localScale = new Vector3(0.15f,0.15f,0.15f);
	_itemDisplayRules[0].localAngles = new Vector3(0f, 180f, 0f);
	_itemDisplayRules[0].localPos = new Vector3(-0.35f, -0.1f, 0f);

	return new CustomItem(newItemDef, _prefab, _icon, _itemDisplayRules);
}
```

You'll want to retrieve the id of your custom item using the name you gave it in the method body. Using the GetItemId  (GetEquipmentId for equipment) method from the library :

#### Hook Example with Custom Item

```csharp
_myCustomItemId = ItemLib.GetItemId("Custom Item Example"); // get the item's id

On.RoR2.CharacterBody.OnKilledOther += (orig, self, damageReport) =>
 {
  orig(self, damageReport);

  if (self.inventory.GetItemCount((ItemIndex) _myCustomItemId) > 0)
  {
		// do stuff
  }
};
```

#### Custom Equipment Method Example
```csharp
[Item(ItemAttribute.ItemType.Equipment)]
public static ItemLib.CustomEquipment Test()
{
	// Load the AssetBundle you made with the Unity Editor

	_exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Rampage_data");

	_prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
	_icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

	EquipmentDef newEquipmentDef = new EquipmentDef
	{
		cooldown = 45f,
		pickupModelPath = "",
		pickupIconPath = "",
		nameToken = "Custom Equipment Example",
		pickupToken = "pickup sample text",
		descriptionToken = "description in logbook",
		canDrop = true,
		enigmaCompatible = true
	};
	
	_itemDisplayRules = new ItemDisplayRule[1]; // keep this null if you don't want the item to show up on the survivor 3d model. You can have multiple rules !
	_itemDisplayRules[0].followerPrefab = _prefab; // the prefab that will show up on the survivor
	_itemDisplayRules[0].childName = "Chest"; // this will define the starting point for the prefab, you can see what are the differents name available in the prefab model of the survivors.
	_itemDisplayRules[0].localScale = new Vector3(0.15f,0.15f,0.15f);
	_itemDisplayRules[0].localAngles = new Vector3(0f, 180f, 0f);
	_itemDisplayRules[0].localPos = new Vector3(-0.35f, -0.1f, 0f);

	return new CustomEquipment(newEquipmentDef, _prefab, _icon);
}
```

#### Hook Example with Custom Equipment

```csharp
_myCustomItemId = ItemLib.ItemLib.GetEquipmentId("Custom Equipment Example");

// Need to hook in here so the item actually proc, the orig method is a switch case on the equipmentIndex

On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentIndex) =>
{
	if ((int) equipmentIndex == _myCustomItemId)
	{
		DetonateAlive(100);
		return true; // must
	}
	return orig(self, equipmentIndex); // must
};
```

#### Custom Buffs
Buffs in RoR2 are status effects, which may or may not be 'buffs' in the beneficial sense.  By creating a custom buff, it will add an array entry to all characters that can track whether that buff is present, will automatically display an icon when the buff is active and also gives you access to handy methods like adding a timed buff to a character that automatically decays.  To create a custom buff, just provide an Icon, buffColor and whether it's stackable.  If no Icon is specified, it will default to a square of the buffColor provided.  To make the buff have effects in the code, hook whatever methods you want to modify, then use CharacterBody.HasBuff to check for its presence.  You can use AddBuff and AddTimedBuff to apply the buff.
```csharp
	[Item(ItemAttribute.ItemType.Buff)]
	public static CustomBuff TestBuff()
	{
		LoadAssets();

		var buffDef = new BuffDef
		{
			buffColor = Color.green,
			canStack = false
		};

		Sprite icon = null; //Can load a custom sprite asset here; null defaults to a blank colored square
		return new CustomBuff("MyBuff", buffDef, icon);
	}
```

#### Custom Elites
Elites in Risk of Rain 2 are internally made up of three key elements.  There is an EliteDef, plus every elite has a custom Equipment, which then passive applies a custom Buff.  To create custom elites, you only need to implement one method and the associated equipment/buff will automatically be created and linked to it.  This method is implemented very much like the custom items and equipment:
```csharp
        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite TestElite()
        {
            LoadAssets();

            var eliteDef = new EliteDef
            {
                modifierToken = "Cloaky",
                color = new Color32(255, 105, 180, 255)
            };
            var equipDef = new EquipmentDef
            {
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = "Cloaky",
                pickupToken = "Cloaky",
                descriptionToken = "Cloaky",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                buffColor = eliteDef.color,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, _prefab, _icon, _itemDisplayRules);
            var buff = new CustomBuff("Affix_Cloaky", buffDef, null);              
            var elite = new CustomElite("Cloaky", eliteDef, equip, buff, 1);
            return elite;
        }
```
Note that we're reusing equipment _prefab and _icon assets from the Equipment example here.  The final parameter of the CustomElite constructor here is the 'tier'.  According to vanila spawning mechanics, tiers 1 and 2 have different modifiers, as well as some additional requirements (tier 2 can only spawn after the first loop.)  You can specify a tier (1 or 2) of you wish to use the vanilla spawning or you can use the overhauled Elite spawning mechanics described below.

#### Elite Spawning Overhaul
The built-in tier system for elites is fairly limiting.  For example, if you want an elite type to be costly, like Malachites, it will also end up having the huge dmg and hp boosts for Malachites, which you might not want in all cases.  ESO provides an alternate mechanism for elite spawning with more flexibility, modeled after the 'SpawnCard' system RoR2 uses.  This feature may be disabled in the configuration for ItemLib, in which case spawning will revert to the vanilla tier-based mechanism.

Once you've defined a custom elite, you can make it eligible for spawning like this:
```csharp
	//Overall, this elite is pretty rare
	//(Though, by the way, it has a built-in sticky bomb)
	var card = new EliteAffixCard
	{
		spawnWeight = 0.1f,				//Only 10% as likely to spawn compared to baseline vanilla elites
		costMultiplier = 6.0f,			//Costs 6x, which is the same as tier 1 elites
		damageBoostCoeff = 2.0f,		//Damage boost the same as vanilla elites
		healthBoostCoeff = 4.7f,		//Health boost the same as vanilla elites
		eliteType = (EliteIndex) eliteId,
		onSpawned = m => m.inventory.GiveItem(ItemIndex.StickyBomb, 1)
	};

	//Except it's really common on beetles for some reason
	card.spawnCardMultipliers.Add("cscbeetle", 20);	//20x more likely than vanilla for this elite type to be chosen on Beetles

	//Add it to the list so that it's available for spawning with ESO
	EliteSpawningOverhaul.Cards.Add(card);
```

Note that there is an onSpawned hook provided that you can use to set up each newly created elite CharacterMaster instance; in this case, the elite monster is granted an item.  To add other types of effects caused by this elite, hook areas of the code you want to change and then use CharacterBody.HasBuff to check for the elite buff.  Do remember that players can get this buff too if they have Wake of Vultures and kill an elite with your new type or if they pickup the rarely dropped elite equipment.

It's possible to create more than one EliteAffixCard for the same elite type, giving the cards different weights, costs, boosts, etc.  This can let you create multiple tiers of your elite types or to add new versions of the vanilla elites with different power levels.  The vanilla elite settings can also be modified by modifying the elements in EliteSpawningOverhaul.Cards, though bear in mind that other mods might try to do the same thing.