SOURCES=./Explorers/IExplorer.cs ./Explorers/FileExplorer.Designer.cs ./Explorers/FileExplorer.cs ./IProgressable.cs ./Properties/AssemblyInfo.cs ./ProgressPopup.cs ./ProgressPopup.Designer.cs 
LIBS=../FileSystems/KFA.FileSystems.dll
RESOURCES=Explorers/GuiComponents.FileExplorer.resources GuiComponents.SaveProgressPopup.resources
KFA.GuiComponents.dll: ${SOURCES} ${LIBS|} ${RESOURCES}
	make -C ../FileSystems
	mcs -t:library -o KFA.GuiComponents.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Data.dll -r:../FileSystems/KFA.FileSystems.dll $(addprefix -resource:,${RESOURCES}) ${SOURCES}
clean:
	rm KFA.GuiComponents.dll
