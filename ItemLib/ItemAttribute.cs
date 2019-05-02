using System;

namespace ItemLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ItemAttribute : Attribute
    {

        /// <summary>
        /// The name of the referenced item.
        /// </summary>
        public string ItemName { get; protected set; }

        /// <param name="ItemName">The name of the referenced item.</param>
        public ItemAttribute(string ItemName)
        {
            this.ItemName = ItemName;
        }
    }
}
