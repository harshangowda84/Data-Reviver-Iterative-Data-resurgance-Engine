using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;

namespace KFA.GUI.Explorers {
    public interface IExplorer {
        bool CanView(IDataStream stream);
        void View(IDataStream stream);
    }
}
