using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuiComponents {
	public interface IProgressable {
		event ProgressEvent Progress;
		event Action Finished;
	}

	public delegate void ProgressEvent(string status, double progress);
}
