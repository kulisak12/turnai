namespace TurnAi {

    /// <summary>All-purpose factory.</summary>
    public interface IFactory<out T> {
        T Create();
    }

    /// <remarks>Follows singleton pattern, use the <c>Instance</c> field.</remarks>
    public class Factory<T> : IFactory<T> where T : new() {
        public static readonly IFactory<T> Instance = new Factory<T>();
        private Factory() { }
        public T Create() => new T();
    }
}
