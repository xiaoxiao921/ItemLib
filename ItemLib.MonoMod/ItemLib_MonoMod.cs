namespace RoR2
{
    internal class patch_ItemCatalog
    {
        public static ItemDef GetItemDef(ItemIndex itemIndex)
        {
            return ItemLib.ItemLib.GetItemDef(itemIndex);
        }
    }
}