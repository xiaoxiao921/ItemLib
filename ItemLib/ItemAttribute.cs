using System;
using UnityEngine;

namespace ItemLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ItemAttribute : Attribute
    {
        public enum ItemType
        {
            Item,
            Equipment
        }

        public readonly ItemType Type;

        /// <param name="type"> Type.Item or Type.Equipment.</param>
        public ItemAttribute(ItemType type)
        {
            Type = type;
        }
    }
}