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
    class Team
    {
        public Color TeamColor;
        public String TeamName;
        public List<Team> Allies;
        public List<Unit> Units;

        public Team(String name, Color color)
        {
            TeamName = name;
            TeamColor = color;

            Allies = new List<Team>();
            Units = new List<Unit>();
        }

        public void AddUnit(Unit u)
        {
            Units.Add(u);
            u.Team = this;
        }
    }
}
