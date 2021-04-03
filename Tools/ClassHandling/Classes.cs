using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Tools.ClassHandler
{
    public abstract class AnonymousClass
    {

    }
    public class AnonymousProperty
    {
        private string name;
        private Type type;

        public AnonymousProperty(string name, Type type)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (type == default(Type)) throw new ArgumentNullException(nameof(type));

            this.name = name;
            this.type = type;
        }

        public string Name => this.name;

        public Type Type => this.type;
    }
    public class AnonymousClassSignature : IEquatable<AnonymousClassSignature>
    {
        private AnonymousProperty[] properties;
        private int hashCode = 0;

        public AnonymousClassSignature(IEnumerable<AnonymousProperty> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            this.properties = properties.ToArray();
            foreach (var property in this.properties)
            {
                this.hashCode = this.hashCode ^ property.Name.GetHashCode() ^ property.Type.GetHashCode();
            }
        }

        public override int GetHashCode() => this.hashCode;

        public override bool Equals(object obj) => obj is AnonymousClassSignature ? this.Equals(obj as AnonymousClassSignature) : false;

        public bool Equals(AnonymousClassSignature other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (this.properties.Length != other.properties.Length) return false;

            for (var i = 0; i < this.properties.Length; i++)
            {
                if (this.properties[i].Name != other.properties[i].Name || this.properties[i].Type != other.properties[i].Type)
                    return false;
            }

            return true;
        }
    }
}
