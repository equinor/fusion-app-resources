namespace Fusion.Resources.Domain.Commands
{
    public class MonitorableProperty<T>
    {
        public T Value { get; }

        public bool HasBeenSet { get; }

        public MonitorableProperty()
        {
        }

        public MonitorableProperty(T value)
        {
            HasBeenSet = true;
            Value = value;
        }

        public MonitorableProperty(T value, bool hasBeenSet)
        {
            HasBeenSet = hasBeenSet;
            Value = value;
        }

        public static implicit operator MonitorableProperty<T>(T value)
        {
            return new MonitorableProperty<T>(value);
        }
    }
}
