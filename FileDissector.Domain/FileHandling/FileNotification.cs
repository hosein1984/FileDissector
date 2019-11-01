using System;
using System.IO;

namespace FileDissector.Domain.FileHandling
{
    /// <summary>
    /// This class represent the result of a file scan based on the "polling" approach.
    /// In the polling approach instead of using the <see cref="FileSystemWatcher"/> class, we regularly using our info of the file 
    /// and based on these updated information return appropriate notification.
    /// <remarks>
    /// The structure of these class is set to use different constructors to get differernt states of the <see cref="FileNotification"/>.
    /// Each of the constructor usages are explained in the following.
    /// </remarks>
    /// </summary>
    public class FileNotification : IEquatable<FileNotification>
    {
        private FileInfo Info { get; }
        public bool Exists { get; }
        public long Size { get; }
        public string FullName => Info.FullName;
        public string Name => Info.Name;
        public string Folder => Info.DirectoryName;
        public FileNotificationType NotificationType { get; }
        public Exception Error { get; }

        /// <summary>
        /// This constructor is used for when we have no prior knowledge of the file notifications. This is used when we first start 
        /// polling changes from the File.
        /// </summary>
        /// <param name="fileInfo"></param>
        public FileNotification(FileInfo fileInfo)
        {
            fileInfo.Refresh(); // in case that the file is changed from the time that the FileInfo object is created
            Info = fileInfo;
            Exists = fileInfo.Exists;

            // since this constructor is used for the start of polling, it only handles "Created" and "Missing" notification types
            if (Exists)
            {
                NotificationType = FileNotificationType.Created;
                Size = Info.Length;
            }
            else
            {
                NotificationType = FileNotificationType.Missing;
            }
        }

        /// <summary>
        /// This constructor is used when we faced an exception while trying to poll information for the file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="error"></param>
        public FileNotification(FileInfo fileInfo, Exception error)
        {
            Info = fileInfo;
            Error = error;
            Exists = false;
            NotificationType = FileNotificationType.Error;
        }

        /// <summary>
        /// This constructor is used when we have already started polling from the file and we have prior knowledge (notifications) about the file.
        /// By comparing the new state with the old state this constructor created the correct notification type
        /// </summary>
        /// <param name="previous"></param>
        public FileNotification(FileNotification previous)
        {
            previous.Info.Refresh(); // in case that the file is changed from the time that the FileInfo object is created

            Info = previous.Info;
            Exists = Info.Exists;

            if (Exists)
            {
                Size = Info.Length;

                if (!previous.Exists)
                {
                    NotificationType = FileNotificationType.Created;
                }
                else if (Size != previous.Size)
                {
                    NotificationType = FileNotificationType.Changed;
                }
                else
                {
                    // this case represent when the file is not changed betweem different polls
                    NotificationType = FileNotificationType.None;
                }
            }
            else
            {
                NotificationType = FileNotificationType.Missing;
            }
        }

        public override string ToString()
        {
            return $"{Name}, Size: {Size}, Type: {NotificationType}";
        }

        #region IEquality

        public bool Equals(FileNotification other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Exists == other.Exists && Size == other.Size && string.Equals(FullName, other.FullName) && NotificationType == other.NotificationType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileNotification)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Exists.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (FullName != null ? FullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)NotificationType;
                return hashCode;
            }
        }

        public static bool operator ==(FileNotification left, FileNotification right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FileNotification left, FileNotification right)
        {
            return !(left == right);
        }

        #endregion


    }
}

