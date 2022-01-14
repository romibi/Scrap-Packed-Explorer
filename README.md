Scrap Packed Explorer
=====================

With this App it is planned to make it userfriendly to create a new or modify the contents of an existing .packed file for the Game Scrapland.

See also the [Scrap Hacks Project by Earthnuker](https://gitdab.com/Earthnuker/ScrapHacks) (on gitdab)  
There is already a CLI implementation to modify packed files using Python (3.5 ?)

---
## Table of contents
* [About "packed" files](#about-packed-files)
* [How to use](#how-to-use)
    * [GUI](#GUI)
	* [CLI](#CLI)
    	* [Add](#Add)
		* [Remove](#Remove)
		* [Rename](#Rename)
		* [Extract](#Extract)
		* [List](#List)
		* [Help](#Help)
		* [Version](#Version)
* [Dependencies](#dependencies)
* [Todo](#todo-not-strictly-ordered-by-priority)

---

## About "packed" files
!TBD!
<!-- No idea how to put it into words -->

---

## How to use
The App can work in 2 different modes: GUI and CLI.   
To run App with GUI you just simply should run the any of distributed *.exe files.
To run App with CLI you must run the ScrapPackedExplorer\<BitsVersion\>Cli.exe with command line arguments.

There is Cli version of distribution because when you launch it without any command line arguments it flashes cmd windows for a split second. If you ok with this - use Cli version. 
Version without Cli supports only GUI mode so it no flashes cmd.
<!-- Man, so hard to write this -->

---

### GUI
!TBD!

---

### CLI
Basic command looks like this:
```bash
ScrapPackedExplorerCli.exe <path-to-the-packed-file> <subcommand> <options>
```
You can specify subcommand first. 

List of the subcommands:

| Subcommand		 | Description										|
|--------------------|--------------------------------------------------|
|[add](#add)		 |Add file to the archive							|
|[remove](#remove)	 |Remove a file from the archive					|
|[rename](#rename)	 |rename a file or folder inside the archive		|
|[extract](#extract) |Extract/unpack a file from the archive			|
|[list](#list)		 |list or search files and folders in the archive	|
|[help](#help)		 |Display more information on a specific command.	|
|[version](#version) |Display version information.						|

---

### Add
Add file to given archive. If given archive is not exists the App will create new one.

| Option                   | Description
|--------------------------|--------------------------------------------------------------------------------------------------------|
|Packed file (pos. 0)      |Required. The .packed file to use as basis																|
|-s, --sourcePath          |Required. What file or folder to add to the .packed file												|
|-d, --packedPath          |(Default: ) What path to put the source file(s) into													|
|-o, --outputPackedFile    |(Default: ) Where to store the new .packed file. Modify input if not provided.							|
|-k, --keepBackup          |(Default: false) Keep the backup file that gets created during saving even after successful processing.	|
|--overwriteOldBackup      |(Default: false) Allow overwriting existing .bak files													|
|--help                    |Display this help screen.																				|
|--version                 |Display version information.																			|

```bash
ScrapPackedExplorerCli.exe exapmle.packed add -s file1.txt -p folder/file.txt
ScrapPackedExplorerCli.exe exapmle.packed add -s folder\subfolder -p folder/
```

---

### Remove
Removes file or folder from given archive.

| Option                   | Description
|--------------------------|--------------------------------------------------------------------------------------------------------|
|Packed file (pos. 0)      |Required. The .packed file to use as basis																|
|-p, --packedPath          |Required. What path to remove from the archive															|
|-o, --outputPackedFile    |(Default: ) Where to store the new .packed file. Modify input if not provided.							|
|-k, --keepBackup          |(Default: false) Keep the backup file that gets created during saving even after successful processing.	|
|--overwriteOldBackup      |(Default: false) Allow overwriting existing .bak files													|
|--help                    |Display this help screen.																				|
|--version                 |Display version information.																			|

```bash
ScrapPackedExplorerCli.exe exapmle.packed remove -p file.txt
ScrapPackedExplorerCli.exe exapmle.packed remove -p folder/file.txt
ScrapPackedExplorerCli.exe exapmle.packed remove -p folder/
```

---

### Rename
Renames file or folder inside of given archive. Can rename path of the file (basicaly move it).

| Option                   | Description
|--------------------------|--------------------------------------------------------------------------------------------------------|
|Packed file (pos. 0)      |Required. The .packed file to use as basis																|
|-s, --oldPackedPath       |Required. (Default: /) What path to rename inside the archive											|
|-d, --newPackedPath       |Required. The new path to use for the files to rename													|
|-o, --outputPackedFile    |(Default: ) Where to store the new .packed file. Modify input if not provided.							|
|-k, --keepBackup          |(Default: false) Keep the backup file that gets created during saving even after successful processing.	|
|--overwriteOldBackup      |(Default: false) Allow overwriting existing .bak files													|
|--help                    |Display this help screen.																				|
|--version                 |Display version information.																			|

```bash
ScrapPackedExplorerCli.exe exapmle.packed rename -s file1.txt -d file2.txt
ScrapPackedExplorerCli.exe exapmle.packed rename -s file1.txt -d folder/file1.txt
```

---

### Extract
Extracts file or folder from given archive to given path. 
If  `-s` option is not specified the App will extract everything.

| Option                   | Description												|
|--------------------------|------------------------------------------------------------|
|Packed file (pos. 0)      |Required. The .packed file to use as basis					|
|-s, --packedPath          |(Default: ) What path to extract from the archive			|
|-d, --destinationPath     |Required. The path to extract the files from the archive to	|
|--help                    |Display this help screen.									|
|--version                 |Display version information.								|

```bash
ScrapPackedExplorerCli.exe exapmle.packed extract -s file1.txt -d file.txt
ScrapPackedExplorerCli.exe exapmle.packed extract -s file1.txt -d out\
ScrapPackedExplorerCli.exe exapmle.packed extract -s folder/subfolder -d outFolder\
ScrapPackedExplorerCli.exe exapmle.packed extract -d out\
```

---

### List
Lists files and folders in given archive.

| Option                   | Description																							
|--------------------------|--------------------------------------------------------------------------------------------------------|
|Packed file (pos. 0)      |Required. The .packed file to use as basis																|
|-l, --outputStyle         |(Default: List) Output list (default) or tree view. Valid values: None, List, Tree, Name     			|
|-q, --searchString        |(Default: ) A Search string to filter the output with													|
|-r, --regex		       |(Default: false) Defines if the search string is a regular expression									|
|-b, --match-beginning     |(Default: false) Apply search query only to beginnng of the files path. By default applies everywhere	|
|-f, --match-filename      |(Default: false) Search only by files. By default search includes folders								|
|-s, --show-file-size      |(Default: false) Show files sizes																		|
|-o, --show-file-offset    |(Default: false) Show files offsets																		|
|--help                    |Display this help screen.																				|
|--version                 |Display version information.																			|

```bash
ScrapPackedExplorerCli.exe exapmle.packed list
ScrapPackedExplorerCli.exe exapmle.packed list -rfq file\.(txt|png) -sol name
```

---

### Help
Prints help message on the screen. If only help provieded it shows avilible subcommands. 
If after `help` another subcommand was provided it will show avilible options for that subcommand.

```bash
ScrapPackedExplorerCli.exe help
ScrapPackedExplorerCli.exe help list
```

---

### Version
Display current version of App																	|

```bash
ScrapPackedExplorerCli.exe version
```
---
## Dependencies
This App is using [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-3.1.22-windows-hosting-bundle-installer) so be sure if it is installed on your computer.  
No other dependencies are needed.

---
## Todo (not strictly ordered by priority)
- [x] Create Project structure
- Command Line Interface
	- [x] mostly done, see issue [#1](/../../issues/1)
- Graphical User Interface _(see also issue [#2](/../../issues/2))_
	- [x] directory tree
	- [x] folder content view
	- [x] add file/folder
	- [ ] replace warning
	- [x] remove file/folder
	- [x] extract file/folder
	- [ ] rename file/folder
	- [ ] search name
	- [ ] search content (maybe)
	- [ ] drag & drop
	- [ ] drag & drop between 2 packed (maybe)
	- [ ] icon
	- [ ] nice loading animation
	- [ ] direct vs prepare modes
    	_Re-create archive on each change or prepare changes and then save_
	- [ ] "modding" mode (maybe)
    	_Have huge gaps between files edited to not move around bits much inside archive while constantly editing_
	- [ ] file preview (maybe)
- Misc: see [other issues](/../../issues/)
