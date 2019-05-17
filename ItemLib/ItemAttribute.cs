using System;

namespace ItemLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ItemAttribute : Attribute
    {
        public enum ItemType
        {
            Item,
            Equipment
        }

        public ItemType Type;
        /// <param name="type"> Type.Item or Type.Equipment.</param>
        public ItemAttribute(ItemType type)
        {
            this.Type = type;
        }
    }
}