I am using .net 9 here.

Open Command/Terminal and run the program as follows (Windows):

--root "C:\Path\To\YourSolution" \
    --output "C:\Temp\CombinedSources.cs" \
    --exclude "*.Designer.cs" "*.g.cs"

In detail, if you have a project folder and want to save the result in your Downloads folder under Windows, and this is my user name "haraldi":

.\ConcatSources.exe --root "C:\Users\haraldi\source\repos\SubstancesTest\Definitions" --output "C:\Users\haraldi\Downloads\ModelProduct\tmp\result.cs"
