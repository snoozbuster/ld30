using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD30
{
    public class PropCategory
    {
        public static List<PropCategory> Categories { get { return new List<PropCategory>(categories); } }
        private static List<PropCategory> categories = new List<PropCategory>();

        public readonly string Name;
        public List<Prop> Props { get { return new List<Prop>(props); } }
        private List<Prop> props;

        public PropCategory(string name, params Prop[] contents)
        {
            if(categories.Exists(v => v.Name == name))
                throw new ArgumentException("Name already exists.", "name");

            props = new List<Prop>();
            Name = name;

            foreach(Prop p in contents)
                Add(p);

            categories.Add(this);
        }

        public void Add(Prop p)
        {
            if(p.Category == this)
                return;

            if(p.Category != null)
                p.Category.Remove(p);

            p.Category = this;
            props.Add(p);
        }

        public void Remove(Prop p)
        {
            if(p.Category != this)
                return;

            p.Category = null;
            props.Remove(p);
        }
    }
}
