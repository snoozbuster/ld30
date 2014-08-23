using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LD30
{
    public class paste
    {
        public string paste_key;
        public int paste_date;
        public string paste_title;
        public int paste_size;
        public int paste_expire_date;
        public int paste_private;
        [XmlIgnore]
        public PastePrivacy privacy { get { return (PastePrivacy)paste_private; } }
        public string paste_format_long;
        public string paste_format_short;
        public string paste_url;
        public int paste_hits;

        public override bool Equals(object obj)
        {
            return obj is paste && paste_key == (obj as paste).paste_key;
        }

        public static bool operator ==(paste lhs, paste rhs)
        {
            return lhs.paste_key == rhs.paste_key;
        }

        public static bool operator !=(paste lhs, paste rhs)
        {
            return !(lhs.paste_key == rhs.paste_key);
        }

        public override int GetHashCode()
        {
            return paste_key.GetHashCode();
        }
    }

    public enum PastePrivacy { Public, Unlisted, Private }
}
