I am using .net 9 here.

# Background

When trying to use different AI tools for programming, it might be a cumbersome work to "merge" all your (here: *.cs) files in one file to upload it to your AI tool of your preference or if you use an API. So, I needed a tool that merges all my files in a folder with subfolder structure in a single file for uploading, as there might be a restriction also in the number of files one can upload and it is horrible to look for each file in each subfolder in a bigger project. Here you can add all.

Take nevertheless care and have a look at the context window size if you upload it.

# Usage

Open Command/Terminal and run the program as follows (Windows):

--root "C:\Path\To\YourSolution" \
    --output "C:\Temp\CombinedSources.cs" \
    --exclude "*.Designer.cs" "*.g.cs"

In detail, if you have a project folder and want to save the result in your Downloads folder under Windows, and this is my user name "haraldi":

.\ConcatSources.exe --root "C:\Users\haraldi\source\repos\SubstancesTest\Definitions" --output "C:\Users\haraldi\Downloads\ModelProduct\tmp\result.cs"

# Comments

Take care:

* Vibe coded with the help of OpenAI (with a paid license, model used o3 today at 2025-08-02).
* I am using "System.CommandLine" in version 2.0.0-beta6.25358.103. One doesn't need that (I am writing helper apps now for a very long time, and I have never used that). It was a vibe proposal, and I liked it, so I have kept it. **Take care: using beta software is always on your own risk!**
* As I am using in many projects Entity Framework Code First, there is in program.cs a list where you can exclude directories you don't want. Here for Entity Framework Core Code First: "Migrations". This is *always* excluded.
