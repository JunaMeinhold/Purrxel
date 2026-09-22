namespace VoxelEngine.Scenes
{
    using Hexa.NET.Mathematics;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    public delegate void GameObjectEventHandler<T>(GameObject sender, T args);

    public class GameObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private readonly List<IComponent> components = [];
        private readonly List<GameObject> children = [];
        private bool initialized = false;
        private Guid guid = Guid.NewGuid();
        private string name = string.Empty;
        private string? fullName;

        private object? tag;
        private GameObject? parent;
        private Transform transform = new();
        private bool enabled = true;

        /// <summary>
        /// Gets the scene that the element is associated.
        /// </summary>
        /// <value>
        /// The scene.
        /// </value>
        public virtual Scene Scene { get; internal set; }

        /// <summary>
        /// Gets the dispatcher of the scene.
        /// </summary>
        /// <value>
        /// The dispatcher of the scene.
        /// </value>
        public SceneDispatcher Dispatcher => Scene.Dispatcher;

        public GameObject? Parent
        {
            get => parent;
            private set
            {
                parent = value;
                ParentChanged?.Invoke(this, value);
            }
        }

        public IReadOnlyList<GameObject> Children => children;

        public IReadOnlyList<IComponent> Components => components;

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        public Guid Guid
        {
            get => guid;
            set
            {
                if (SetAndNotifyWithEqualsTest(ref guid, value))
                {
                    fullName = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get => name;
            set
            {
                if (SetAndNotifyWithEqualsTest(ref name, value))
                {
                    fullName = null;
                    NameChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the full name of the <see cref="GameObject"/>, which is a unique combination of its name and Guid.
        /// The full name is lazily generated when accessed for the first time and will be
        /// reinitialized if either the name or Guid property is modified.
        /// </summary>
        /// <value>
        /// The full name of the <see cref="GameObject"/>.
        /// </value>
        public string FullName
        {
            get
            {
                return fullName ??= $"{name}##{guid}";
            }
        }

        public object? Tag
        {
            get => tag;
            set
            {
                if (SetAndNotifyWithEqualsTest(ref tag, value))
                {
                    TagChanged?.Invoke(this, value);
                }
            }
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (SetAndNotifyWithEqualsTest(ref enabled, value))
                {
                    EnabledChanged?.Invoke(this, value);
                }
            }
        }

        public event GameObjectEventHandler<bool>? EnabledChanged;

        public event GameObjectEventHandler<string>? NameChanged;

        public event GameObjectEventHandler<IComponent>? ComponentAdded;

        public event GameObjectEventHandler<IComponent>? ComponentRemoved;

        public event GameObjectEventHandler<object?>? TagChanged;

        public event GameObjectEventHandler<GameObject?>? ParentChanged;

        public event GameObjectEventHandler<GameObject>? ChildAdded;

        public event GameObjectEventHandler<GameObject>? ChildRemoved;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event PropertyChangingEventHandler? PropertyChanging;

        public event GameObjectEventHandler<Transform>? TransformUpdated;

        public event GameObjectEventHandler<Transform>? TransformChanged;

        public Transform Transform { get => transform; set => OverwriteTransform(transform); }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        protected void NotifyPropertyChanging([CallerMemberName] string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new(propertyName));
        }

        protected bool SetAndNotifyWithEqualsTest<T>(ref T field, T value, [CallerMemberName] string name = "") where T : IEquatable<T>
        {
            if (field.Equals(value))
            {
                return false;
            }

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }

        protected bool SetAndNotifyWithEqualsTest(ref object? field, object? value, [CallerMemberName] string name = "")
        {
            if (field == value)
            {
                return false;
            }

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }

        protected void SetAndNotify<T>(ref T target, T value, [CallerMemberName] string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new(propertyName));
            target = value;
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        /// <summary>
        /// Initializes this instance.<br/>
        /// </summary>
        public virtual void Awake()
        {
            Transform.Changed += OnTransformChanged;
            Transform.Updated += OnTransformUpdated;
            foreach (var component in components)
            {
                component.GameObject = this;
            }
            foreach (var child in Children)
            {
                child.Parent = this;
                child.Scene = Scene;
                child.Awake();
            }
            Scene.Register(this);
            initialized = true;
        }

        /// <summary>
        /// Uninitializes this instance.<br/>
        /// </summary>
        public virtual void Destroy()
        {
            Transform.Changed -= OnTransformChanged;
            Transform.Updated -= OnTransformUpdated;
            initialized = false;
            for (int i = 0; i < components.Count; i++)
            {
                components[i].Destroy();
            }
            foreach (var child in Children)
            {
                child.Destroy();
            }
            Scene.Unregister(this);
            Scene = null!;
        }

        protected void OverwriteTransform(Transform transform)
        {
            this.transform.Updated -= OnTransformUpdated;
            this.transform.Changed -= OnTransformChanged;
            this.transform = transform;
            transform.Updated += OnTransformUpdated;
            transform.Changed += OnTransformChanged;
        }

        protected virtual void OnTransformChanged(Transform transform)
        {
            TransformChanged?.Invoke(this, transform);
        }

        protected virtual void OnTransformUpdated(Transform transform)
        {
            TransformUpdated?.Invoke(this, transform);
        }

        public bool IsAncestorOf(GameObject ancestor)
        {
            for (GameObject? current = Parent; current != null; current = current.Parent)
            {
                if (current == ancestor) return true;
            }
            return false;
        }

        public virtual void AddChild(GameObject child)
        {
            if (child == this)
            {
                throw new InvalidOperationException("A GameObject cannot be its own parent.");
            }

            if (IsAncestorOf(child))
            {
                throw new InvalidOperationException("A GameObject cannot be its own ancestor.");
            }

            child.Parent?.RemoveChild(child);
            child.Parent = this;

            children.Add(child);

            if (initialized)
            {
                child.Scene = Scene;
                child.Awake();
            }

            ChildAdded?.Invoke(this, child);
        }

        public virtual bool RemoveChild(GameObject child)
        {
            if (!children.Remove(child))
            {
                return false;
            }

            if (initialized)
            {
                child.Destroy();
            }
            child.Parent = null;

            ChildRemoved?.Invoke(this, child);

            return true;
        }

        /// <summary>
        /// Binds an component to SceneElement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component">The component.</param>
        public virtual void AddComponent(IComponent component)
        {
            components.Add(component);
            if (initialized)
            {
                component.GameObject = this;
            }
            ComponentAdded?.Invoke(this, component);
        }

        public virtual void RemoveComponent(IComponent component)
        {
            components.Remove(component);
            ComponentRemoved?.Invoke(this, component);
        }

        /// <summary>
        /// Gets an Component by T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T or null</returns>
        public virtual T? GetComponent<T>() where T : IComponent
        {
            foreach (var component in components)
            {
                if (component is T t)
                {
                    return t;
                }
            }

            return default;
        }

        /// <summary>
        /// Tries to get component by T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component">T or null</param>
        /// <returns>true if sucess, false if failed</returns>
        public virtual bool TryGetComponent<T>([MaybeNullWhen(false)] out T? component) where T : IComponent
        {
            foreach (var componentT in components)
            {
                if (componentT is T t)
                {
                    component = t;
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Gets all T components from this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetComponents<T>() where T : IComponent
        {
            foreach (var component in components)
            {
                if (component is T t)
                {
                    yield return t;
                }
            }
        }

        public IEnumerable<IComponent> GetComponents(Func<IComponent, bool> selector)
        {
            foreach (var component in components)
            {
                if (selector(component))
                {
                    yield return component;
                }
            }
        }

        public IEnumerable<T> GetComponents<T>(Func<T, bool> selector) where T : IComponent
        {
            foreach (var component in components)
            {
                if (component is T t && selector(t))
                {
                    yield return t;
                }
            }
        }
    }
}