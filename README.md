I am using .net 9 here.

# Usage

Open Command/Terminal and run the program as follows (Windows):

--root "C:\Path\To\YourSolution" \
    --output "C:\Temp\CombinedSources.cs" \
    --exclude "*.Designer.cs" "*.g.cs"

In detail, if you have a project folder and want to save the result in your Downloads folder under Windows, and this is my user name "haraldi":

.\ConcatSources.exe --root "C:\Users\haraldi\source\repos\SubstancesTest\Definitions" --output "C:\Users\haraldi\Downloads\ModelProduct\tmp\result.cs"

# Comments

Take care:

* vibe coded with the help of OpenAI.
* I am using "System.CommandLine" in version 2.0.0-beta6.25358.103. One doesn't need that (I am writing helper apps now for a very long time, and I have never used that). It was a proposal, and I liked it, so I have kept it. **Take care: using beta software is always on your own risk!**
