using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public interface IBreadCrumb
    {
        Dictionary<string, string> keyValues { get; set; }
        string screenname { get; set; }

        bool Equals(BreadCrumb other);
        bool Equals(object obj);
        int GetHashCode();
    }

    public class BreadCrumb : IEquatable<BreadCrumb>, IBreadCrumb
    {
        public BreadCrumb()
        {

        }
        public string screenname { get; set; }
        public Dictionary<string, string> keyValues { get; set; } = new Dictionary<string, string>();

        public override bool Equals(object obj)
        {
            return Equals(obj as BreadCrumb);
        }

        public bool Equals(BreadCrumb other)
        {
            return other != null &&
                   screenname == other.screenname &&
                   EqualityComparer<Dictionary<string, string>>.Default.Equals(keyValues, other.keyValues);
        }

        public override int GetHashCode()
        {
            int hashCode = 1569004476;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(screenname);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(keyValues);
            return hashCode;
        }

        public static bool operator ==(BreadCrumb left, BreadCrumb right)
        {
            return EqualityComparer<BreadCrumb>.Default.Equals(left, right);
        }

        public static bool operator !=(BreadCrumb left, BreadCrumb right)
        {
            return !(left == right);
        }
    }
}
