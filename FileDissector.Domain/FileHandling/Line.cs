using System;

namespace FileDissector.Domain.FileHandling
{
    /// <summary>
    /// Represents a Line in a file
    /// </summary>
    public class Line : IEquatable<Line>
    {
        /// <summary>
        /// Line numebr
        /// </summary>
        public int Number { get; }
        
        /// <summary>
        /// Line text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Represents the time the line is loaded in the app which if using small poll intervals is almost the same as the when the line is created
        /// </summary>
        public DateTime? Timestamp { get; }

        public Line(int number, string text, DateTime? timestamp)
        {
            Number = number;
            Text = text;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"{Number}: {Text}";
        }

        #region Equality

        public bool Equals(Line other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Number == other.Number && string.Equals(Text, other.Text);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Line) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Number * 397) ^ (Text != null ? Text.GetHashCode() : 0);
            }
        }

        #endregion
    }
}
