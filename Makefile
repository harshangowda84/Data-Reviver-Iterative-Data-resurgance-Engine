SOURCES=./KickassUndelete/Properties/Settings.Designer.cs ./KickassUndelete/Properties/Resources.Designer.cs ./KickassUndelete/Properties/AssemblyInfo.cs ./KickassUndelete/ScanState.cs ./KickassUndelete/Program.cs ./KickassUndelete/DeletedFileViewer.Designer.cs ./KickassUndelete/ListViewColumnSorter.cs ./KickassUndelete/ExtensionMethods.cs ./KickassUndelete/MainForm.Designer.cs ./KickassUndelete/MainForm.cs ./KickassUndelete/DeletedFileViewer.cs ./KickassUndelete/ConsoleCommands.cs
LIBS=FileSystems/KFA.FileSystems.Lite.dll GuiComponents/KFA.GuiComponents.dll
RESOURCES=KickassUndelete/KickassUndelete.MainForm.resources KickassUndelete/KickassUndelete.DeletedFileViewer.resources
.SUFFIXES:
.SUFFIXES: .resx .resources
.resx.resources:
	resgen $<
KickassUndelete.exe: ${LIBS} ${SOURCES} ${RESOURCES}
	make -C FileSystems
	make -C GuiComponents
	mcs -d:MONO -o KickassUndelete.exe -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Data.dll $(addprefix -r:,${LIBS}) $(addprefix -resource:,${RESOURCES}) ${SOURCES}
clean:
	rm KickassUndelete.exe
