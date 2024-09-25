using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornadoSAR
{
    internal class ShowMain : Button
    {

        private Main _main = null;

        protected override void OnClick()
        {
            //already open?
            if (_main != null)
                return;
            _main = new Main();
            _main.Owner = FrameworkApplication.Current.MainWindow;
            _main.Closed += (o, e) => { _main = null; };
            _main.Show();
            //uncomment for modal
            //_main.ShowDialog();
        }

    }
}
