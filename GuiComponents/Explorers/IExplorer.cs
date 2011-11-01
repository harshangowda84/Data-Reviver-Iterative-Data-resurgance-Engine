using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;

namespace KFA.GUI.Explorers {
    /// <summary>
    /// The interface for an explorer widget.
    /// </summary>
    public interface IExplorer {

        /// <summary>
        /// Gets whether this explorer can view a particular stream.
        /// </summary>
        /// <param name="stream">The stream to view.</param>
        /// <returns>Whether the stream can be viewed by this explorer.</returns>
        bool CanView(IDataStream stream);

        /// <summary>
        /// Views a data stream. Clients should check that CanView(stream)
        /// returns true before calling this method.
        /// </summary>
        /// <param name="stream">The data stream to view.</param>
        void View(IDataStream stream);
    }
}
