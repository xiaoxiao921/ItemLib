using System;

namespace ItemLib
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ItemAttribute : Attribute
    {
        public enum ItemType
        {
            Item,
            Equipment,
            Buff,
            Elite
        }

        public readonly ItemType Type;

        public ItemAttribute(ItemType type)
        {
            Type = type;
        }
    }
}