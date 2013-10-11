﻿namespace Avalonia.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    internal class PropertyPathWalker
    {
        static readonly PropertyInfo[] CollectionViewProperties = typeof(ICollectionView).GetProperties();

        public object Source
        {
            get;
            private set;
        }

        public event EventHandler IsBrokenChanged;

        public event EventHandler ValueChanged;

        public IPropertyPathNode FinalNode
        {
            get;
            private set;
        }

        bool IsDataContextBound
        {
            get;
            set;
        }

        public bool IsPathBroken
        {
            get
            {
                if (IsDataContextBound && string.IsNullOrEmpty(Path))
                    return false;

                var node = Node;
                while (node != null)
                {
                    if (node.IsBroken)
                        return true;
                    node = node.Next;
                }
                return false;
            }
        }

        IPropertyPathNode Node
        {
            get;
            set;
        }

        string Path
        {
            get;
            set;
        }

        public object Value
        {
            get;
            private set;
        }

        public PropertyPathWalker(string path)
            : this(path, true, false, false)
        {

        }

        public PropertyPathWalker(string path, bool bindDirectlyToSource, bool bindsToView, bool isDataContextBound)
        {
            IsDataContextBound = isDataContextBound;

            string index;
            string propertyName;
            string typeName;
            PropertyNodeType type;
            CollectionViewNode lastCVNode = null;
            Path = path;

            if (string.IsNullOrEmpty(path) || path == ".")
            {
                // If the property path is null or empty, we still need to add a CollectionViewNode
                // to handle the case where we bind diretly to a CollectionViewSource. i.e. new Binding () { Source = cvs }
                // An empty path means we always bind directly to the view.
                Node = new CollectionViewNode(bindDirectlyToSource, bindsToView);
                lastCVNode = (CollectionViewNode)Node;
                FinalNode = Node;
            }
            else
            {
                var parser = new PropertyPathParser(path);
                while ((type = parser.Step(out typeName, out propertyName, out index)) != PropertyNodeType.None)
                {
                    bool isViewProperty = CollectionViewProperties.Any(prop => prop.Name == propertyName);
                    IPropertyPathNode node = new CollectionViewNode(bindDirectlyToSource, isViewProperty);
                    lastCVNode = (CollectionViewNode)node;
                    switch (type)
                    {
                        case PropertyNodeType.AttachedProperty:
                        case PropertyNodeType.Property:
                            node.Next = new StandardPropertyPathNode(typeName, propertyName);
                            break;
                        case PropertyNodeType.Indexed:
                            node.Next = new IndexedPropertyPathNode(index);
                            break;
                        default:
                            throw new Exception("Unsupported node type");
                    }

                    if (FinalNode != null)
                        FinalNode.Next = node;
                    else
                        Node = node;

                    FinalNode = node.Next;
                }
            }

            lastCVNode.BindToView |= bindsToView;
            FinalNode.IsBrokenChanged += delegate(object o, EventArgs e)
            {
                Value = ((PropertyPathNode)o).Value;
                var h = IsBrokenChanged;
                if (h != null)
                    h(this, EventArgs.Empty);
            };
            FinalNode.ValueChanged += delegate(object o, EventArgs e)
            {
                Value = ((PropertyPathNode)o).Value;
                var h = ValueChanged;
                if (h != null)
                    h(this, EventArgs.Empty);
            };
        }

        public object GetValue(object item)
        {
            Update(item);
            object o = FinalNode.Value;
            Update(null);
            return o;
        }

        public void Update(object source)
        {
            Source = source;
            Node.SetSource(source);
        }
    }

}