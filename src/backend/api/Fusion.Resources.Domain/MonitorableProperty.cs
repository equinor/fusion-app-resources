using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class MonitorableProperty<T>
    {
        public T Value { get; }

        public bool HasBeenSet { get; }

        public MonitorableProperty()
        {
            HasBeenSet = false;
            Value = default!;
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

    public static class MonitorablePropertyExtensions
    {
        public static bool IfSet<TValue>(this MonitorableProperty<TValue> property, Action<TValue> action)
        {
            bool hasChanges = false;

            if (property.HasBeenSet)
            {
                action(property.Value);
                hasChanges = true;
            }

            return hasChanges;
        }

        public static async Task<bool> IfSetAsync<TValue>(this MonitorableProperty<TValue> property, Func<TValue, Task> action)
        {
            bool hasChanges = false;

            if (property.HasBeenSet)
            {
                await action(property.Value);
                hasChanges = true;
            }

            return hasChanges;
        }
    }
}
