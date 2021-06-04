set fso= CreateObject("Scripting.FileSystemObject")

set folder = fso.GetFolder("D:\4_cours\diplom\diplom\object-core\assets\train-data\car")
Wscript.Echo "Beggin"
i = 0
for each file in folder.Files
		i = i + 1
   		file.Move(file.ParentFolder & "\new\car" & i & "." & fso.getextensionname(file.path))
Next