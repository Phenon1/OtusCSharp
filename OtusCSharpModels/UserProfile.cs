
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtusCSharpModels
{
    [GenerateBinarySerializer]
    public partial class UserProfile : IEquatable<UserProfile>, ISerializableBinary<UserProfile>
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime CreatedOn { get; set; }

        public UserProfile(string Username) 
        { 
            this.Username = Username;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as UserProfile);
        }

        public bool Equals(UserProfile? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Id == other.Id &&
                   Username == other.Username &&
                   CreatedOn == other.CreatedOn;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Username, CreatedOn);
        }

        public static bool operator ==(UserProfile? left, UserProfile? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(UserProfile? left, UserProfile? right)
        {
            return !(left == right);
        }

    }


}
