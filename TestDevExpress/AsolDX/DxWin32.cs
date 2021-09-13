using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Konstanty Win32
    /// </summary>
    public class DxWin32
    {
        /// <summary>
        /// Konstanty pro proceduru BitBlt
        /// </summary>
        public class BitBlt
        {
            /// <summary>
            /// Copies the source rectangle directly to the destination rectangle.
            /// </summary>
            public const int SRCCOPY = 0x00CC0020; /* dest = source                   */
            /// <summary>
            /// Combines the colors of the source and destination rectangles by using the Boolean OR operator.
            /// </summary>
            public const int SRCPAINT = 0x00EE0086; /* dest = source OR dest           */
            /// <summary>
            /// Combines the colors of the source and destination rectangles by using the Boolean AND operator.
            /// </summary>
            public const int SRCAND = 0x008800C6; /* dest = source AND dest          */
            /// <summary>
            /// Combines the colors of the source and destination rectangles by using the Boolean XOR operator.
            /// </summary>
            public const int SRCINVERT = 0x00660046; /* dest = source XOR dest          */
            /// <summary>
            /// Combines the inverted colors of the destination rectangle with the colors of the source rectangle by using the Boolean AND operator.
            /// </summary>
            public const int SRCERASE = 0x00440328;/* dest = source AND (NOT dest )   */
            /// <summary>
            /// Copies the inverted source rectangle to the destination.
            /// </summary>
            public const int NOTSRCCOPY = 0x00330008;/* dest = (NOT source)             */
            /// <summary>
            /// Combines the colors of the source and destination rectangles by using the Boolean OR operator and then inverts the resultant color.
            /// </summary>
            public const int NOTSRCERASE = 0x001100A6;/* dest = (NOT src) AND (NOT dest) */
            /// <summary>
            /// Merges the colors of the source rectangle with the brush currently selected in hdcDest, by using the Boolean AND operator.
            /// </summary>
            public const int MERGECOPY = 0x00C000CA;/* dest = (source AND pattern)     */
            /// <summary>
            /// Merges the colors of the inverted source rectangle with the colors of the destination rectangle by using the Boolean OR operator.
            /// </summary>
            public const int MERGEPAINT = 0x00BB0226;/* dest = (NOT source) OR dest     */
            /// <summary>
            /// Copies the brush currently selected in hdcDest, into the destination bitmap.
            /// </summary>
            public const int PATCOPY = 0x00F00021;/* dest = pattern                  */
            /// <summary>
            /// Combines the colors of the brush currently selected in hdcDest, with the colors of the inverted source rectangle by using the Boolean OR operator. 
            /// The result of this operation is combined with the colors of the destination rectangle by using the Boolean OR operator.
            /// </summary>
            public const int PATPAINT = 0x00FB0A09;/* dest = DPSnoo                   */
            /// <summary>
            /// Combines the colors of the brush currently selected in hdcDest, with the colors of the destination rectangle by using the Boolean XOR operator.
            /// </summary>
            public const int PATINVERT = 0x005A0049;/* dest = pattern XOR dest         */
            /// <summary>
            /// Inverts the destination rectangle.
            /// </summary>
            public const int DSTINVERT = 0x00550009;/* dest = (NOT dest)               */
            /// <summary>
            /// Fills the destination rectangle using the color associated with index 0 in the physical palette. (This color is black for the default physical palette.)
            /// </summary>
            public const int BLACKNESS = 0x00000042;/* dest = BLACK                    */
            /// <summary>
            /// Fills the destination rectangle using the color associated with index 1 in the physical palette. (This color is white for the default physical palette.)
            /// </summary>
            public const int WHITENESS = 0x00FF0062;/* dest = WHITE                    */
            /// <summary>
            /// Prevents the bitmap from being mirrored.
            /// </summary>
            public const uint NOMIRRORBITMAP = 0x80000000;/* Do not Mirror the bitmap in this call */
            /// <summary>
            /// Includes any windows that are layered on top of your window in the resulting image. 
            /// By default, the image only contains your window. 
            /// Note that this generally cannot be used for printing device contexts.
            /// </summary>
            public const int CAPTUREBLT = 0x40000000;/* Include layered windows */
        }
    }
}
