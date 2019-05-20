# ItemLib
A library for custom items and equipments in Risk of Rain 2.

### Table of Contents

- [Installation](https://github.com/xiaoxiao921/ItemLib#usage)
- [Project Setup](https://github.com/xiaoxiao921/ItemLib#project-setup)

## Installation

ItemLib is a BepinEx plugin and a Monomod patch, that will also be used as an assembly reference for your BepinEx Plugin or Unity Assetmod. (MultiMod)

### Installing ItemLib

- [Install BepInEx](https://thunderstore.io/package/bbepis/BepInExPack)
- Download the latest [itemlib.zip](https://github.com/xiaoxiao921/ItemLib/releases)
- Extract the zip to your `BepInEx/plugins/` folder

## Project Setup

### Building from source

- Clone / Download as zip the repo
- Open `ItemLib.sln`

You may have to fix the assembly reference `ItemLib.dll` for the ExampleItemMod project.

### Using the library

Once your project is ready you'll want to have a method defined that will return a CustomItem / CustomEquipment object and have an Item Attribute at the top so that the library can load it. \
Depending on what you want (item or equipment) you'll want to change the ItemType (enum) in the attribute. \
Leave both `pickupModelPath` and/or `pickupIconPath` empty if you want to have a custom prefab / icon for your item. \
For having a custom prefab and icon you will need to make an AssetBundle in Unity.


#### Custom Item Method Example
```csharp
[Item(ItemAttribute.ItemType.Item)]
public static ItemLib.CustomItem Example()
{
  // Load the AssetBundle you made with the Unity Editor

	_exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/exampleitemmod");

	_prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
	_icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

	ItemDef newItemDef = new ItemDef
	{
		tier = ItemTier.Tier1,
		pickupModelPath = "", // leave it empty and give directly the prefab / icon on the return but you can also use an already made prefab by putting a path in there.
		pickupIconPath = "",
		nameToken = "Custom Item Example",
		pickupToken = "i'm a custom item. i do sticky bomb on kill",
		descriptionToken = "yes",
		addressToken = ""
	};

	return new CustomItem(newItemDef, _prefab, _icon);
}
```

You'll want to retrieve the id of your custom item using the name you gave it in the method body. Using the GetItemId method from the library :

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
