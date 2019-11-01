using System;

namespace FileDissector.Domain.FileHandling
{
    [Flags]
    public enum FileNotificationType
    {
        None    = 1, // not changed
        Created = 2, // file created
        Changed = 4, // size of the file is changed
        Missing = 8, // while is missing 
        Error   = 16 // had error retreiving data about file
    }
}
