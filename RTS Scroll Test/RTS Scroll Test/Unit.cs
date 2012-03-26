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
        //identification
        static int _id = 0;
        public int ID;
        public Team Team;
        //collision
        public Rectangle HardCollisionBox;
        public Rectangle SoftCollisionBox;
        //movement
        public int Speed;
        public Vector2 Destination;
        private Vector2 _last;
        //drawing
        public Texture2D Texture;
        public Texture2D Portrait;
        public Texture2D ActionTexture;
        public Rectangle DrawArea
        {
            get
            {
                return HardCollisionBox;
            }
        }
        //position
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

                w = SoftCollisionBox.Width;
                h = SoftCollisionBox.Height;

                SoftCollisionBox = new Rectangle(((int)value.X - w / 2), ((int)value.Y - h / 2), w, h);
            }
        }


        public Unit(int x, int y, int w, int h, Texture2D texture, Texture2D portrait)
        {
            ID = _id++;

            HardCollisionBox = new Rectangle(0, 0, w, h);
            SoftCollisionBox = new Rectangle(0, 0, w + (w / 4), h + (h / 4));

            Center = new Vector2(x, y);

            Destination = new Vector2(x, y);
            Speed = 10; //default unit speed

            Texture = texture;
            Portrait = portrait;
        }

        public Unit(int x, int y, int w, int h, Texture2D t, Texture2D p, Texture2D action)
            : this(x, y, w, h, t, p)
        {
            ActionTexture = action;
        }

        public Boolean CollidesWith(Unit u)
        {
            return u.HardCollisionBox.Intersects(HardCollisionBox);
        }
        public Boolean Arrived()
        {
            return Vector2.Distance(Destination, Center) < Speed;
        }
        public void Update()
        {
            if (Arrived())
            {
                Center = Destination;
            }
            else
            {
                Vector2 path = Destination - Center;
                path.Normalize();
                _last = Center;
                Center += path * Speed;
            }
        }
        public void UndoUpdate()
        {
            Center = _last;
        }
        public Boolean IsMoving()
        {
            return !(Center == _last && Center != Destination);
        }
        public virtual void Action(int x, int y, List<Unit> units)
        {
            Destination = new Vector2(x, y);
        }
    }
}
