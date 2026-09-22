namespace Purrxel.Engine
{
    using Purrxel.Engine.Scenes;
    using System;
    using System.Linq;

    public static class Extensions
    {
        public static void RemoveAllComponents<T>(this List<T> ts, GameObject element) where T : IComponent
        {
            IEnumerable<T> comp = element.GetComponents<T>();
            ts.RemoveAll(x => comp.Contains(x));
        }

        public static void AddAllComponents<T>(this List<T> ts, GameObject element) where T : IComponent
        {
            ts.AddRange(element.GetComponents<T>());
        }
    }
}