using System;
using System.Threading;

namespace Fusion.Resources.Domain.Behaviours
{
    /// <summary>
    /// Scope that will replace the reliance on http context to determine the edit, set by the auditable command behaviour.
    /// The scope should be used when the system initiate the change.
    /// 
    /// The system will act as Guid.Empty as azure id, for tracability.
    /// </summary>
    public class SystemEditorScope : IDisposable
    {
        public static AsyncLocal<bool> IsEnabled { get; set; } = new AsyncLocal<bool>();

        public SystemEditorScope()
        {
            IsEnabled.Value = true;
        }

        public void Dispose()
        {
            IsEnabled.Value = false;
        }
    }
}
