using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDevExpress.Components
{
    internal class Constants
    {
        #region Basic colors

        internal static Tuple<string, string> ColorPairGreen { get { return new Tuple<string, string>(ColorGreen, ColorGreenLight); } }
        internal static Tuple<string, string> ColorPairRed { get { return new Tuple<string, string>(ColorRed, ColorRedLight); } }
        internal static Tuple<string, string> ColorPairYellow { get { return new Tuple<string, string>(ColorYellow, ColorYellowLight); } }
        internal static Tuple<string, string> ColorPairOrange { get { return new Tuple<string, string>(ColorOrange, ColorOrangeLight); } }

        internal static Tuple<string, string> ColorPairBlue { get { return new Tuple<string, string>(ColorBlue, ColorBlueLight); } }
        internal static Tuple<string, string> ColorPairTurquoise { get { return new Tuple<string, string>(ColorTurquoise, ColorTurquoiseLight); } }
        internal static Tuple<string, string> ColorPairPurple { get { return new Tuple<string, string>(ColorPurple, ColorPurpleLight); } }
        internal static Tuple<string, string> ColorPairBrown { get { return new Tuple<string, string>(ColorBrown, ColorBrownLight); } }

        internal static Tuple<string, string> ColorPairGrey { get { return new Tuple<string, string>(ColorGrey, ColorGreyLight); } }
        internal static Tuple<string, string> ColorPairBlack { get { return new Tuple<string, string>(ColorBlack, ColorWhite); } }

        /// <summary>
        /// Barva černá
        /// </summary>
        internal const string ColorBlack = "#000000"; // ALE0077576

        /// <summary>
        /// Barva bílá
        /// </summary>
        internal const string ColorWhite = "#FFFFFF"; // ALE0077576

        /// <summary>
        /// Barva šedá
        /// </summary>
        internal const string ColorGrey = "#383838"; // ALE0077576

        /// <summary>
        /// Barva šedá - světlá
        /// </summary>
        internal const string ColorGreyLight = "#C8C6C4"; // ALE0077576

        /// <summary>
        /// Barva červená
        /// </summary>
        internal const string ColorRed = "#E42D2C"; // ALE0077576

        /// <summary>
        /// Barva červená - světlá
        /// </summary>
        internal const string ColorRedLight = "#F3B8B8"; // ALE0077576

        /// <summary>
        /// Barva oranžová
        /// </summary>
        internal const string ColorOrange = "#E57428"; // ALE0077576

        /// <summary>
        /// Barva oranžová - světlá
        /// </summary>
        internal const string ColorOrangeLight = "#F7CDA7"; // ALE0077576

        /// <summary>
        /// Barva žlutá
        /// </summary>
        internal const string ColorYellow = "#E57428"; // ALE0077576

        /// <summary>
        /// Barva žlutá - světlá
        /// </summary>
        internal const string ColorYellowLight = "#F7DA8E"; // ALE0077576

        /// <summary>
        /// Barva zelená
        /// </summary>
        internal const string ColorGreen = "#0BA04A"; // ALE0077576

        /// <summary>
        /// Barva zelená - světlá
        /// </summary>
        internal const string ColorGreenLight = "#ACD8B1"; // ALE0077576

        /// <summary>
        /// Barva tyrkysová
        /// </summary>
        internal const string ColorTurquoise = "#21B4C9"; // ALE0077576

        /// <summary>
        /// Barva tyrkysová - světlá
        /// </summary>
        internal const string ColorTurquoiseLight = "#BEE2E5"; // ALE0077576

        /// <summary>
        /// Barva modrá
        /// </summary>
        internal const string ColorBlue = "#0964B0"; // ALE0077576

        /// <summary>
        /// Barva modrá - světlá
        /// </summary>
        internal const string ColorBlueLight = "#92CBEE"; // ALE0077576

        /// <summary>
        /// Barva fialová
        /// </summary>
        internal const string ColorPurple = "#A0519F"; // ALE0077576

        /// <summary>
        /// Barva fialová - světlá
        /// </summary>
        internal const string ColorPurpleLight = "#DFBCD9"; // ALE0077576

        /// <summary>
        /// Barva hnědá
        /// </summary>
        internal const string ColorBrown = "#9B5435"; // ALE0077576

        /// <summary>
        /// Barva hnědá - světlá
        /// </summary>
        internal const string ColorBrownLight = "#DDAE85"; // ALE0077576
        #endregion
    }
}
