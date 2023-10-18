Scrap Packed Explorer
=====================

This App allows you to create or modify the contents of .packed files for the Game Scrapland.

See also the [Scrap Hacks Project by Earthnuker](https://gitdab.com/Earthnuker/ScrapHacks) (on gitdab) for an alternative implementation using python

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
.packed Files are used by the Game Scrapland to have most of the game data merged into one big data container.
The contents are neither encrypted nor compressed. Therefore modifying it is relatively straight forward.

But before this project there was no real userfriendly way to do that and the goal of this project was to enable modding for users who do not really want to think about the details of this filetype.

---

## How to use
The App can work in 2 different modes: GUI and CLI. (`Graphical User Interface` vs `Command Line Interface`)
To run App with GUI you just simply should run any of the distributed *.exe files.
To run App with CLI you must run ScrapPackedExplorerCli.exe (or ScrapPackedExplorerCli32.exe) with command line arguments.

Although the CLI version also contains the whole GUI, a separate CLI version is distributed, because when you launch the GUI (by not providing any command line arguments) a cmd window flashes for a split second.
If you are ok with this you can use Cli version for both GUI and CLi mode.

The Version without Cli in its name supports only the GUI mode but also has no flashing cmd window on startup.

---

### GUI
!TBD!
GUI is nearly usuable but some key features are missing for fully functional usage.
Updating this README chapter is part of the GUI issue (#2)

---

### CLI
A basic call looks like this:

```bash
ScrapPackedExplorerCli.exe <path-to-the-packed-file> <subcommand> <options>
```
You can also specify the subcommand before the path to the packed file.

List of the subcommands:

| Subcommand           | Description                                                                               |
| -------------------- | ------------------------------------------------------------------------------------------|
| [add](#add)          | Add file to the container                                                                 |
| [remove](#remove)    | Remove a file from the container                                                          |
| [rename](#rename)    | rename a file or folder inside the container                                              |
| [extract](#extract)  | Extract/unpack a file from the container                                                  |
| [list](#list)        | List or search files and folders in the container                                         |
| [cat](#cat)          | Print content of file inside of containerist or search files and folders in the container |
| [help](#help)        | Display more information on a specific command.                                           |
| [version](#version)  | Display version information.                                                              |

---

### Add
Add a file or folder to a given container. If the container does not exists the App will create new one.

| Option                     | Description                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------- |
| Packed file (pos. 0)       | Required. The .packed file to use as basis                                                               |
| -s, --source-path          | Required. What file or folder to add to the .packed file                                                 |
| -d, --packed-path          | (Default: ) What path to put the source file(s) into                                                     |
| -o, --output-packed-file   | (Default: ) Where to store the new .packed file. Modify input if not provided.                           |
| -k, --keep-backup          | (Default: false) Keep the backup file that gets created during saving even after successful processing.  |
| --overwrite-old-backup     | (Default: false) Allow overwriting existing .bak files                                                   |
| --help                     | Display this help screen.                                                                                |
| --version                  | Display version information.                                                                             |

```bash
ScrapPackedExplorerCli.exe exapmle.packed add -s file1.txt -p folder/file.txt
ScrapPackedExplorerCli.exe exapmle.packed add -s folder\subfolder -p folder/
```

---

### Remove
Removes a file or folder from the given container.

| Option                     | Description                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------- |
| Packed file (pos. 0)       | Required. The .packed file to use as basis                                                               |
| -d, --packed-path          | Required. What path to remove from the container                                                         |
| -o, --output-packed-file   | (Default: ) Where to store the new .packed file. Modify input if not provided.                           |
| -k, --keep-backup          | (Default: false) Keep the backup file that gets created during saving even after successful processing.  |
| --overwrite-old-backup     | (Default: false) Allow overwriting existing .bak files                                                   |
| --help                     | Display this help screen.                                                                                |
| --version                  | Display version information.                                                                             |

```bash
ScrapPackedExplorerCli.exe exapmle.packed remove -p file.txt
ScrapPackedExplorerCli.exe exapmle.packed remove -p folder/file.txt
ScrapPackedExplorerCli.exe exapmle.packed remove -p folder/
```

---

### Rename
Renames file or folder inside the given container. Also used to change the path of the file (to basicaly move it).

| Option                     | Description                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------- |
| Packed file (pos. 0)       | Required. The .packed file to use as basis                                                               |
| -s, --old-packed-path      | Required. (Default: /) What path to rename inside the container                                          |
| -d, --new-packed-path      | Required. The new path to use for the files to rename                                                    |
| -o, --output-packed-file   | (Default: ) Where to store the new .packed file. Modify input if not provided.                           |
| -k, --keep-backup          | (Default: false) Keep the backup file that gets created during saving even after successful processing.  |
| --overwrite-old-backup     | (Default: false) Allow overwriting existing .bak files                                                   |
| --help                     | Display this help screen.                                                                                |
| --version                  | Display version information.                                                                             |

```bash
ScrapPackedExplorerCli.exe exapmle.packed rename -s file1.txt -d file2.txt
ScrapPackedExplorerCli.exe exapmle.packed rename -s file1.txt -d folder/file1.txt
```

---

### Extract
Extracts a file or folder from the given container to a given path.
If option `-s` is not specified, the App will extract everything.

| Option                     | Description                                                   |
| -------------------------- | ------------------------------------------------------------  |
| Packed file (pos. 0)       | Required. The .packed file to use as basis                    |
| -s, --packed-path          | (Default: ) What path to extract from the container           |
| -d, --destination-path     | Required. The path to extract the files from the container to |
| --help                     | Display this help screen.                                     |
| --version                  | Display version information.                                  |

```bash
ScrapPackedExplorerCli.exe exapmle.packed extract -s file1.txt -d file.txt
ScrapPackedExplorerCli.exe exapmle.packed extract -s file1.txt -d out\
ScrapPackedExplorerCli.exe exapmle.packed extract -s folder/subfolder -d outFolder\
ScrapPackedExplorerCli.exe exapmle.packed extract -d out\
```

---

### List
Lists files and folders in the given container.

| Option                     | Description                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------- |
| Packed file (pos. 0)       | Required. The .packed file to use as basis                                                               |
| -l, --output-style         | (Default: List) Output list (default) or tree view. Valid values: None, List, Tree, Name                 |
| -s, --show-file-size       | (Default: false) Show files sizes                                                                        |
| -o, --show-file-offset     | (Default: false) Show files offsets                                                                      |
| --no-errors                | (Default: false) Disable error messages                                                                  |
| -q, --search-string        | (Default: ) A Search string to filter the output with                                                    |
| -r, --regex                | (Default: false) Defines if the search string is a regular expression                                    |
| -b, --match-beginning      | (Default: false) Apply search query only to beginnng of the files path. By default applies everywhere    |
| -f, --match-filename       | (Default: false) Search only by files. By default search includes folders                                |
| --help                     | Display this help screen.                                                                                |
| --version                  | Display version information.                                                                             |

```bash
ScrapPackedExplorerCli.exe exapmle.packed list
ScrapPackedExplorerCli.exe exapmle.packed list -rfq file\.(txt|png) -sol name
```

---

### Cat

Print content of file inside of containerist or search files and folders in the container

| Option                       | Description                                                    |
| --------------------------   | -------------------------------------------------------------- |
| Packed file (pos. 0)         | Required. The .packed file to use as basis                     |
| -s, --packed-path            | Required. (Default: ) What file to print                       |
| -x, --as-hex                 | (Default: false) Display file content as hex dump              |
| -f, --byte-format            | (Default: X2) Format of printed bytes                          |
| -l, --line-format            | (Default: X8) Format of lines numbers                          |
| -g, --bytes-per-group        | (Default: 2) How much bytes should print before printing space |
| -r, --groups-per-row         | (Default: 16) How much groups should print in one line         |
| -p, --no-print-lines-numbers | (Default: false) Do not print lines numbers                    |
| --help                       | Display this help screen.                                      |
| --version                    | Display version information.                                   |

---

### Help
Prints a help message on the screen. Without further parameters it shows the available subcommands.
If after `help` another subcommand is provided it will show avilible options for that subcommand.

```bash
ScrapPackedExplorerCli.exe help
ScrapPackedExplorerCli.exe help list
```

---

### Version
Display the current version of the App

```bash
ScrapPackedExplorerCli.exe version
```
---
## Dependencies
This App is using [.NET Core 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-6.0.1-windows-hosting-bundle-installer) which is often already installed on modern windows computers. If you have problems launching the App, make sure that it is installed on your computer.
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
    	_Re-create container on each change or prepare changes and then save_
	- [ ] "modding" mode (maybe)
    	_Have huge gaps between files edited to not move around bits much inside container while constantly editing_
	- [ ] file preview (maybe)
- Misc: see [other issues](/../../issues/)
