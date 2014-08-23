using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD30
{
    public class Editor
    {
        public World EditableWorld { get; private set; }

        public bool IsOpen { get; private set; }

        private EditorState state; 
        private enum EditorState { Placing, Menu }

        public Editor(World w)
        {
            EditableWorld = w;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {

        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {

        }
    }
}
