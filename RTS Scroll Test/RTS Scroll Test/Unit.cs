using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RTS_Scroll_Test
{
    class Unit
    {
        public Rectangle HardCollisionBox;
        public Rectangle SoftCollisionBox;
        public int Speed;
        public Vector2 Destination;
        public Texture2D Texture;

        public Rectangle DrawArea
        {
            get
            {
                return HardCollisionBox;
            }
        }
        private Vector2 _center;
        public Vector2 Center
        {
            get
            {
                if (_center == null)
                {
                    _center = new Vector2(HardCollisionBox.X + HardCollisionBox.Width / 2, HardCollisionBox.Y + HardCollisionBox.Height / 2);
                }
                return _center;
            }
            set
            {
                int w = HardCollisionBox.Width;
                int h = HardCollisionBox.Height;

                _center = value;

                HardCollisionBox = new Rectangle(((int)value.X - w / 2), ((int)value.Y - h / 2), w, h);
            }
        }


        public Unit(int x, int y, int w,int h,Texture2D texture)
        {
            HardCollisionBox = new Rectangle(0, 0, w, h);
            Center = new Vector2(x, y);
            Destination = new Vector2(x, y);
            Speed = 5;
            Texture = texture;
        }

        public Boolean CollidesWith(Unit u)
        {
            return u.HardCollisionBox.Intersects(HardCollisionBox);
        }
        public Boolean Buffers(Unit u)
        {
            return u.SoftCollisionBox.Intersects(SoftCollisionBox);
        }
        public Boolean Arrived()
        {
            return HardCollisionBox.Contains((int)Destination.X, (int)Destination.Y);
        }
        public void Update()
        {
            if (Vector2.Distance(Destination, Center) < Speed)
            {
                Center = Destination;
            }
            else
            {
                Vector2 path = Destination - Center;
                path.Normalize();
                Center += path * Speed;
            }
        }
    }
}
