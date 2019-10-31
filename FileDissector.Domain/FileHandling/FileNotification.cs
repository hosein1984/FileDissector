﻿using System;
using System.IO;

namespace FileDissector.Domain.FileHandling
{
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

        public FileNotification(FileInfo fileInfo)
        {
            fileInfo.Refresh();
            Info = fileInfo;
            Exists = fileInfo.Exists;

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

        public FileNotification(FileInfo fileInfo, Exception error)
        {
            Info = fileInfo;
            Error = error;
            Exists = false;
            NotificationType = FileNotificationType.Error;
        }

        public FileNotification(FileNotification previous)
        {
            previous.Info.Refresh();

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

