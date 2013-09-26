// -----------------------------------------------------------------------
// <copyright file="SelectionChangedEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

    /// <summary>
    /// Provides data for the SelectionChanged event. 
    /// </summary>
    public class SelectionChangedEventArgs : RoutedEventArgs
    {
        static readonly object[] EmptyArgs = new object[0];

        private object[] addedItems;
        private object[] removedItems;

        /// <summary> 
        /// Initializes a new instance of a SelectionChangedEventArgs class.
        /// </summary> 
        /// <param name="removedItems">The items that were unselected during this event.</param>
        /// <param name="addedItems">The items that were selected during this event.</param>
        public SelectionChangedEventArgs(IList removedItems, IList addedItems)
        {
            if (null == removedItems)
            {
                throw new ArgumentNullException("removedItems");
            }
            if (null == addedItems)
            {
                throw new ArgumentNullException("addedItems");
            }

            this.removedItems = new object[removedItems.Count];
            removedItems.CopyTo(this.removedItems, 0);
            this.addedItems = new object[addedItems.Count];
            addedItems.CopyTo(this.addedItems, 0);
        }

        /// <summary> 
        /// Initializes a new instance of a SelectionChangedEventArgs class.
        /// </summary> 
        /// <param name="removedItems">The item that was unselected during this event.</param>
        /// <param name="addedItems">The item that was selected during this event.</param>
        internal SelectionChangedEventArgs(object removedItem, object addedItem)
            : this(MakeList(removedItem), MakeList(addedItem))
        {
        }

        /// <summary> 
        /// Gets a list that contains the items that were selected during this event.
        /// </summary> 
        public IList AddedItems
        {
            get { return this.addedItems; }
        }

        /// <summary> 
        /// Gets a list that contains the items that were unselected during this event.
        /// </summary> 
        public IList RemovedItems
        {
            get { return this.removedItems; }
        }

        /// <summary>
        /// Makes a list from an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>The list.</returns>
        private static IList MakeList(object o)
        {
            return o == null ? EmptyArgs : new[] { o };
        }
    }
}
