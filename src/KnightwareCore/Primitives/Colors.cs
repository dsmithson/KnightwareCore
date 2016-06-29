using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Primitives
{
    public static class Colors
    {
        public static Color Black { get { return Color.FromRgb(0, 0, 0); } }
        public static Color White { get { return Color.FromRgb(255, 255, 255); } }
        public static Color Red { get { return Color.FromRgb(255, 0, 0); } }
        public static Color Green { get { return Color.FromRgb(0, 255, 0); } }
        public static Color Blue { get { return Color.FromRgb(0, 0, 255); } }
    }
}
