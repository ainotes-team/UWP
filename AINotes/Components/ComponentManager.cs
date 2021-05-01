using System;
using System.Collections.Generic;
using AINotes.Components.Implementations;
using AINotes.Components.Tools;

namespace AINotes.Components {
    public static class ComponentManager {
        public static readonly List<Type> InstalledComponentTools = new List<Type> {
            typeof(TextComponentTool),
            typeof(HandwritingTool),
            typeof(EraserTool),
            typeof(SelectionComponentTool),
            typeof(ImageComponentTool),
        };

        public static readonly Dictionary<string, Type> ComponentTypesByName = new Dictionary<string, Type> {
            {nameof(TextComponent), typeof(TextComponent)},
            {nameof(ImageComponent), typeof(ImageComponent)},
            {nameof(SimpleComponent), typeof(SimpleComponent)},
        };
    }
}