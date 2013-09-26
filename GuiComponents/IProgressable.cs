using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuiComponents {
	/// <summary>
	/// An object that reports its progress in completing some action.
	/// </summary>
	public interface IProgressable {
		/// <summary>
		/// Fires whenever a progress update is made.
		/// </summary>
		event ProgressEvent Progress;
		/// <summary>
		/// Fires when the process is complete.
		/// </summary>
		event Action Finished;
	}

	/// <summary>
	/// A type of event that occurs when progress has been made.
	/// </summary>
	/// <param name="status">A text description of the current progress.</param>
	/// <param name="progress">
	///		A number from 0.0 to 1.0 representing the current progress.
	///	</param>
	public delegate void ProgressEvent(string status, double progress);
}
