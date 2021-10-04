using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Noris.Clients.Win.Components.Tests
{
    using Noris.Clients.Win.Components.AsolDX;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Testy
    /// </summary>
    [TestClass]
    public class DrawingExtensionsTest
    {
        [TestMethod]
        public void TestAlignMonitors()
        {
            Rectangle b1 = new Rectangle(50, 50, 200, 200);
            Rectangle r1 = AsolDX.DrawingExtensions.FitIntoMonitors(b1);

            Rectangle b2 = new Rectangle(2500, 50, 200, 200);
            Rectangle r2 = AsolDX.DrawingExtensions.FitIntoMonitors(b2);

            Rectangle b3 = new Rectangle(-2500, 50, 200, 200);
            Rectangle r3 = AsolDX.DrawingExtensions.FitIntoMonitors(b3);

            Rectangle b4 = new Rectangle(2500, -4000, 200, 200);
            Rectangle r4 = AsolDX.DrawingExtensions.FitIntoMonitors(b4);

        }

    }
}
