using System;

namespace FileDissector.Domain.FileHandling
{
    [Flags]
    public enum FileNotificationType
    {
        None    = 1,
        Created = 2,
        Changed = 4,
        Missing = 8,
        Error   = 16
    }
}
