using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WireWorld3dot0
{
    public class Screen
    {
        public Point size;

        public int width
        {
            get
            {
                return size.X;
            }
        }

        public int height
        {
            get => size.Y;
        }
        private Point _position;
        public Point position
        {
            get => _position;
            set => _position = value;
        }
        public Screen(int width, int height)
        {
            this.size = new Point(width, height);
            position = new Point(0, 0);
        }

        public Screen(Point size)
        {
            this.size = size;
            position = new Point(0, 0);
        }

        public void Move(int x, int y)
        {
            _position.X += x;
            _position.Y += y;
        }
    }
}
