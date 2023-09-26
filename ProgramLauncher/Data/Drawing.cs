using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Souřadnice.
    /// Po zadání hodnot lze získat konkrétní souřadnice metodou 
    /// </summary>
    public struct RectangleExt
    {
        /// <summary>
        /// Souřadnice Left, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k levé hraně.
        /// Pokud je null, pak je vázaný k pravé hraně nebo na střed.
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Souřadnice Top, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k horní hraně.
        /// Pokud je null, pak je vázaný k dolní hraně nebo na střed.
        /// </summary>
        public int? Top { get; set; }
        /// <summary>
        /// Souřadnice Right, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k pravé hraně.
        /// Pokud je null, pak je vázaný k levé hraně nebo na střed.
        /// </summary>
        public int? Right { get; set; }
        /// <summary>
        /// Souřadnice Bottom, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k dolní hraně.
        /// Pokud je null, pak je vázaný k horní hraně nebo na střed.
        /// </summary>
        public int? Bottom { get; set; }
        /// <summary>
        /// Pevná šířka, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou šířku.
        /// Pokud je null, pak je vázaný k pravé i levé hraně a má šířku proměnnou.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Pevná výška, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou výšku.
        /// Pokud je null, pak je vázaný k horní i dolní hraně a má výšku proměnnou.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Metoda vrátí konkrétní souřadnice v daném prostoru parenta.
        /// Při tom jsou akceptovány plovoucí souřadnice.
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <returns></returns>
        public Rectangle GetBounds(Rectangle parentBounds)
        {





        }
    }
}
