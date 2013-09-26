// -----------------------------------------------------------------------
// <copyright file="StyleTypedPropertyAttribute.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia
{
    using System;

    public class StyleTypedPropertyAttribute : Attribute
    {
        public string Property { get; set; }

        public Type StyleTargetType { get; set; }
    }
}
