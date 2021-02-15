using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using DevExpress.XtraTabbedMdi;


namespace TestDevExpress
{
    public class MdiManager : XtraTabbedMdiManager
    {
        public MdiManager()
        {
            BeginFloating += OnBeginFloating;
            

        }


        #region Fixed Page : některá stránka může být zafoxovaná !!!
        // This example demonstrates how to lock a page to prevent it from being floated, dragged and closed.
        private XtraMdiTabPage _FixedPage;
        public XtraMdiTabPage FixedPage
        {
            get { return _FixedPage; }
            set
            {
                _FixedPage = value;
                InitFixedPage();
            }
        }
        public void SetFixedForm(Form form)
        {
            form.MdiParent = MdiParent;
            form.Show();
            FixedPage = PageByForm(form);
        }
        private XtraMdiTabPage PageByForm(Form form)
        {
            foreach (XtraMdiTabPage page in Pages)
            {
                if (page.MdiChild == form)
                {
                    return page;
                }
            }
            return null;
        }
        private void InitFixedPage()
        {
            if (_FixedPage != null)
            {
                _FixedPage.ShowCloseButton = DevExpress.Utils.DefaultBoolean.False;
                CheckFixedPage();
            }
        }
        protected override void DoDragDrop()
        {
            if (SelectedPage == FixedPage)
                return;
            base.DoDragDrop();
            CheckFixedPage();
        }

        private void CheckFixedPage()
        {
            if (_FixedPage == null)
                return;
            if (Pages.IndexOf(FixedPage) != 0)
            {
                Pages.Remove(FixedPage);
                Pages.Insert(0, FixedPage);
                LayoutChanged();
            }
        }
        void OnBeginFloating(object sender, FloatingCancelEventArgs e)
        {
            e.Cancel = PageByForm(e.ChildForm) == FixedPage;
        }
        #endregion
    }
}
