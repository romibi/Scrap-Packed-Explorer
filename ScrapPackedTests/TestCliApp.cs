using ch.romibi.Scrap.Packed.Explorer.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ch.romibi.Scrap.Packed.PackerLib.Tests {
    [TestClass]
    public class TestCliApp {
        // Note: if some tests fail for no reason cleanup TestData folder in the output folder
        // Todo: ensure that this is not needed

        [TestInitialize]
        public void TestInitialize() {
            if (Directory.Exists("TestResults"))
                Directory.Delete("TestResults", true);
            /*
            if (!Directory.Exists("TestResults") && false)
                Directory.CreateDirectory("TestResults");
            */
            /*
            if(!Directory.Exists(@"TestResults\TestAdd"))
                Directory.CreateDirectory(@"TestResults\TestAdd");

            if (!Directory.Exists(@"TestResults\TestAdd"))
                Directory.CreateDirectory(@"TestResults\TestAdd"); */
        }


        // Test cases
        [TestMethod]
        public void TestRunAddFile() {
            Directory.CreateDirectory(@"TestResults\TestAdd");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestAdd\packedFile.packed", true);

            // add file new
            CheckRunCompareFile(new[] {"add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileNew.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to new"
            );

            // add file existing to root
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExistingToRoot.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing to root"
            );

            // add file existing
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder/file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExisting.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing");

            // add file replace
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile3.txt",
                "--packedPath", "folder/file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileReplace.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing and replace");

            // add file different output
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder\\file.txt",
                "--outputPackedFile", @"TestResults\TestAdd\newPackedFile.packed" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExisting.packed",
                @"TestResults\TestAdd\newPackedFile.packed",
                "Store to different output");
            AssertFilesEqual(@"TestData\TestReferenceFiles\TestAdd\addFileReplace.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Store to different output: previous file modified");
        }

        [TestMethod]
        public void TestRunAddFolder() {
            // add folder new
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder1\" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderNew.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to new");

            // add folder existing root
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder2\" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderExistingToRoot.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to root");

            // add folder existing subfolder
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder1\",
                "--packedPath", "subfolder/" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderExistingToSubfolder.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to subfolder");

            // add folder replace some
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder2\",
                "--packedPath", "subfolder/" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderReplaceSome.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to subfolder, replace some");

            // Reset packedFile.packed
            File.Copy(@"TestData\empty.packed", @"TestResults\TestAdd\packedFile.packed", true);

            // add folder nested sub-folders
            CheckRunCompareFile(new[] { "add", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder4\" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderNestedSubfolders.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder with nested sub-folders to new");
        }

        [TestMethod]
        public void TestRunAddFailed() {
            // add file missing
            CheckRunFail(new[] { "add", @"TestResults\TestAddFail\packedFile.packed",
                "--sourcePath", "exampleFile_missing.txt",
                "--packedPath", "file.txt"},
                1, "Expected file not found");

            // add folder, folder not found
            CheckRunFail(new[] { "add", @"TestResults\TestAddFail\packedFile.packed",
                "--sourcePath", "exampleFolder_missing/",
                "--packedPath", "subfolder/"},
                1, "Expected file not found");

            // add file, file not readable
            Directory.CreateDirectory(@"TestResults\TestAddFail");
            FileStream fsFile = new(@"TestResults\TestAddFail\examplefile_readprotected.txt", FileMode.OpenOrCreate);
            try {
                CheckRunFail(new[] { "add", @"TestResults\TestAddFail\packedFile.packed",
                "--sourcePath", "examplefile_readprotected.txt",
                "--packedPath", "file.txt"},
                1, "expected file not accessible");
            } finally {
                fsFile.Close();
            }

            // add folder, some files not readable
            Directory.CreateDirectory(@"TestResults\TestAddFail\exampleFolder_readprotected");

            File.Copy(@"TestData\examplefile1.txt", @"TestResults\TestAddFail\exampleFolder_readprotected\examplefile_notprotected.txt");
            fsFile = new FileStream(@"TestResults\TestAddFail\exampleFolder_readprotected\examplefile_readprotected.txt", FileMode.OpenOrCreate);

            try {
                CheckRunFail(new[] { "add", @"TestResults\TestAddFail\packedFile.packed",
                "--sourcePath", "exampleFolder_readprotected/",
                "--packedPath", "subfolder/"},
                    1, "expected some file not found");
            } finally {
                fsFile.Close();
            }

            // Not sure if it is needed
            // Directory.Delete("TestResults");
        }

        [TestMethod]
        public void TestRunRemove() {
            Directory.CreateDirectory(@"TestResults\TestRemove");
            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);

            CheckRunCompareFile(new[] { "remove", @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "file1.txt"},
                @"TestData\TestReferenceFiles\TestRemove\removedFile.packed",
                @"TestResults\TestRemove\packedFile.packed",
                "Remove file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);
            CheckRunCompareFile(new[] { "remove", @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "folder1/"},
                @"TestData\TestReferenceFiles\TestRemove\removedFolder.packed",
                @"TestResults\TestRemove\packedFile.packed",
                "Remove folder");

            // Note: remove root does not work and will not be made to work, just create a new packed
            // Todo: Check correctness of statement above
        }

        [TestMethod]
        public void TestRunRemoveFailed() {
            Directory.CreateDirectory(@"TestResults\TestRemove");
            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);

            CheckRunFail(new[] { "remove", @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "file_missing.txt"},
                1, "Remove file does not exist");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);
            CheckRunFail(new[] { "remove", @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "folder_missing/"},
                1, "Remove folder does not exist");
        }

        [TestMethod]
        public void TestRunRename() {
            Directory.CreateDirectory(@"TestResults\TestRename");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "file1.txt",
                "--newPackedPath", "file_renamed.txt"},
                @"TestData\TestReferenceFiles\TestRename\renameFile.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "folder1/",
                "--newPackedPath", "directory/"},
                @"TestData\TestReferenceFiles\TestRename\renameFolder.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename folder");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "/",
                "--newPackedPath", "sub/"},
                @"TestData\TestReferenceFiles\TestRename\renameRoot.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename root");
        }

        [TestMethod]
        public void TestRunRenameFailed() {
            Directory.CreateDirectory(@"TestResults\TestRename");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunFail(new[] { "rename", @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "file_missing.txt",
                "--newPackedPath", "file_renamed.txt"},
                1, "Rename missing file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunFail(new[] { "rename", @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "folder_missing/",
                "--newPackedPath", "directory/"},
                1, "Rename missing folder");
        }

        [TestMethod]
        public void TestRunExtract() {
            CheckRunCompareFile(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "file1.txt",
                "--destinationPath", @"TestResults\TestExtract\file.txt"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractFile\file.txt",
                @"TestResults\TestExtract\file.txt",
                "Extract file");

            CheckRunCompareFolder(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "folder1/",
                "--destinationPath", @"TestResults\TestExtract\someFolder\"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractFolder\someFolder\",
                @"TestResults\TestExtract\someFolder\",
                "Extract folder");

            CheckRunCompareFolder(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "folder2/folder1/file1.txt",
                "--destinationPath", @"TestResults\TestExtract\ExtractFileToFolder\Output\"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractFileToFolder\Output\",
                @"TestResults\TestExtract\ExtractFileToFolder\Output\",
                "Extract file from folder to folder");

            CheckRunCompareFolder(new[] { "extract", @"TestData\example.packed",
                "--destinationPath", @"TestResults\TestExtract\all\"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractAll\",
                @"TestResults\TestExtract\all\",
                "Extract all");
        }

        [TestMethod]
        public void TestRunExtractFailed() {
            CheckRunFail(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "not_exsits.none",
                "--destinationPath", @"TestResults\TestExtract\not_exists.none"},
                1, "Extract nonexisting file");

            CheckRunFail(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "not_exsits/",
                "--destinationPath", @"TestResults\TestExtract\all\"},
                1, "Extract nonexisting folder");

            Directory.CreateDirectory(@"TestResults\TestExtract");
            FileStream fsFile = new(@"TestResults\TestExtract\file.txt", FileMode.OpenOrCreate);
            try {
                CheckRunFail(new[] { "extract", @"TestData\example.packed",
                "--packedPath", "file1.txt",
                "--destinationPath", @"TestResults\TestExtract\file.txt"},
                1, "Destination path is unavilable");
            } finally {
                fsFile.Close();
            }
        }

        [TestMethod]
        public void TestRunList() {
            CheckRunCompareErrorOutput(new[] { "list", @"TestData\empty.packed" },
                "'TestData\\empty.packed' is empty.\r\n",
                "List empty"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed" },
                "file1.txt\r\n" +
                "file2.txt\r\n" +
                "folder1/file1.txt\r\n" +
                "folder1/file2.png\r\n" +
                "folder2/file1.txt\r\n" +
                "folder2/file2.txt\r\n" +
                "folder2/folder1/file1.txt\r\n" +
                "folder2/folder1/file2.txt\r\n",
                "List full"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--showFileSize"},
                "file1.txt\tSize: 104\r\n" +
                "file2.txt\tSize: 171\r\n" +
                "folder1/file1.txt\tSize: 456\r\n" +
                "folder1/file2.png\tSize: 500\r\n" +
                "folder2/file1.txt\tSize: 249\r\n" +
                "folder2/file2.txt\tSize: 167\r\n" +
                "folder2/folder1/file1.txt\tSize: 282\r\n" +
                "folder2/folder1/file2.txt\tSize: 85\r\n",
                "List full filesizes"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--showFileOffset"},
                "file1.txt\tOffset: 244\r\n" +
                "file2.txt\tOffset: 348\r\n" +
                "folder1/file1.txt\tOffset: 519\r\n" +
                "folder1/file2.png\tOffset: 975\r\n" +
                "folder2/file1.txt\tOffset: 1475\r\n" +
                "folder2/file2.txt\tOffset: 1724\r\n" +
                "folder2/folder1/file1.txt\tOffset: 1891\r\n" +
                "folder2/folder1/file2.txt\tOffset: 2173\r\n",
                "List full offsets"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--showFileSize", "--showFileOffset"},
                "file1.txt\tSize: 104\tOffset: 244\r\n" +
                "file2.txt\tSize: 171\tOffset: 348\r\n" +
                "folder1/file1.txt\tSize: 456\tOffset: 519\r\n" +
                "folder1/file2.png\tSize: 500\tOffset: 975\r\n" +
                "folder2/file1.txt\tSize: 249\tOffset: 1475\r\n" +
                "folder2/file2.txt\tSize: 167\tOffset: 1724\r\n" +
                "folder2/folder1/file1.txt\tSize: 282\tOffset: 1891\r\n" +
                "folder2/folder1/file2.txt\tSize: 85\tOffset: 2173\r\n",
                "List full filesizes + offsets"
            );

            CheckRunCompareErrorOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "nothing"},
                "Could not find anything by query 'nothing' in 'TestData\\example.packed'\r\n",
                "List could not find"
            );

            CheckRunCompareErrorOutput(new[] { "list", @"TestData\example.packed",
                "--noErrors", "--searchString", "nothing"},
                "",
                "List could not find no errors"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "1" },
                "file1.txt\r\n" +
                "folder1/file1.txt\r\n" +
                "folder1/file2.png\r\n" +
                "folder2/file1.txt\r\n" +
                "folder2/folder1/file1.txt\r\n" +
                "folder2/folder1/file2.txt\r\n",
                "List 1"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "file", "--matchBeginning" },
                "file1.txt\r\n" +
                "file2.txt\r\n",
                "List file matchBeginning"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\TestReferenceFiles\TestList\listMatchFile.packed",
                "--searchString", "file1", "--matchFilename" },
                "file1.txt\r\n" +
                "folder1/file1.txt\r\n",
                "List matchFilename"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "*.txt" },
                "file1.txt\r\n" +
                "file2.txt\r\n" +
                "folder1/file1.txt\r\n" +
                "folder2/file1.txt\r\n" +
                "folder2/file2.txt\r\n" +
                "folder2/folder1/file1.txt\r\n" +
                "folder2/folder1/file2.txt\r\n",
                "List *.txt"
            );

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "folder2/*.txt" },
                "folder2/file1.txt\r\n" +
                "folder2/file2.txt\r\n" +
                "folder2/folder1/file1.txt\r\n" +
                "folder2/folder1/file2.txt\r\n",
                "List folder2/*.txt");

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", @"folder2/.*\.txt", "--regex" },
                "folder2/file1.txt\r\n" +
                "folder2/file2.txt\r\n" +
                "folder2/folder1/file1.txt\r\n" +
                "folder2/folder1/file2.txt\r\n",
                "List folder2/.*\\.txt");

            // todo: tree output
            //CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
            //    "--outputStyle", "tree"},
            //    "│   file1.txt\r\n" +
            //    "│   file2.txt\r\n" +
            //    "│   \r\n" +
            //    "├───folder1\r\n" +
            //    "│       file1.txt\r\n" +
            //    "│       file2.png\r\n" +
            //    "│   \r\n" +
            //    "└───folder2\r\n" +
            //    "    │   file1.txt\r\n" +
            //    "    │   file2.txt\r\n" +
            //    "    │   \r\n" +
            //    "    └───folder1\r\n" +
            //    "            file1.txt\r\n" +
            //    "            file2.txt\r\n",
            //    "List as tree");

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--outputStyle", "name",
                "--searchString", "folder2/" },
                "file1.txt\r\n" +
                "file2.txt\r\n" +
                "file1.txt\r\n" +
                "file2.txt\r\n",
                "List files with only filename from folder2");

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--outputStyle", "Name",
                "--searchString", "folder2/",
                "--showFileSize", "--showFileOffset"},
                "file1.txt\tSize: 249\tOffset: 1475\r\n" +
                "file2.txt\tSize: 167\tOffset: 1724\r\n" +
                "file1.txt\tSize: 282\tOffset: 1891\r\n" +
                "file2.txt\tSize: 85\tOffset: 2173\r\n",
                "List files with only filename from folder2 + sizes + offsets");
        }

        // note: list may fail only if input .packed file is not correct.
        //       This is covered by `TestInputPackedFail()` test.
        // todo: think about necessity of this test
        //[TestMethod]
        //public void TestRunListFailed()
        //{
        //    Assert.Fail("check not implemented");
        //    // todo: think about failed list calls
        //}

        [TestMethod]
        public void TestInputPackedFail() {
            // check uncorrect input
            CheckRunFail(new[] {"add", "/.,*&^$Q*",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt"}, 1, "unable to create expected file");

            // check nonexisted output
            CheckRunFail(new[] {"remove", "nonexsited.packed",
                    "--packedPath", "file.txt"}, 1, "expected file to not exists");

            if (!Directory.Exists(@"TestResults\TestInputPackedFail"))
                Directory.CreateDirectory(@"TestResults\TestInputPackedFail");
            if (File.Exists(@"TestResults\TestInputPackedFail\packedFile.packed"))
                File.Delete(@"TestResults\TestInputPackedFail\packedFile.packed");

            // check inaccessable packed
            FileStream fsFile = new(@"TestResults\TestInputPackedFail\packedFile.packed", FileMode.OpenOrCreate);
            try {
                CheckRunFail(new[] {"add", @"TestResults\TestInputPackedFail\packedFile.packed",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt"}, 1, "Expected file to be inaccessible");
            } finally {
                byte[] someContent = new[] { (byte)'H', (byte)'i' };
                fsFile.Write(someContent);
                fsFile.Close();
            }

            // check unreadable/invalid packed
            CheckRunFail(new[] {"add", @"TestResults\TestInputPackedFail\packedFile.packed",
                "--sourcePath", @"TestData\exampleFile1.txt",
                "--packedPath", "file.txt"}, 1, "Expected packed file to be invalid");

            if (File.Exists(@"TestResults\TestInputPackedFail\packedFile.packed"))
                File.Delete(@"TestResults\TestInputPackedFail\packedFile.packed");
        }

        [TestMethod]
        public void TestOutputPackedFail() {
            Directory.CreateDirectory(@"TestResults\TestOutputPackedFail");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestOutputPackedFail\packedFile.packed", true);

            // Access denied
            Directory.CreateDirectory(@"TestResults\TestOutputPackedFail\filenameWasTaken");
            CheckRunFail(new[] { "add", @"TestResults\TestOutputPackedFail\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder/file.txt",
                "--outputPackedFile", @"TestResults\TestOutputPackedFail\filenameWasTaken" },
                1, "Access to output file is denied");

            Directory.Delete(@"TestResults\TestOutputPackedFail\filenameWasTaken");

            // check inaccessable output packed
            FileStream fsFile = new(@"TestResults\TestOutputPackedFail\packedOutFile.packed", FileMode.OpenOrCreate);
            try {
                CheckRunFail(new[] {"add", @"TestResults\TestOutputPackedFail\packedFile.packed",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt",
                    "--outputPackedFile", @"TestResults\TestOutputPackedFail\packedOutFile.packed" },
                    1, "Expected output file to be inaccessible");
            } finally {
                fsFile.Close();
            }

            if (Directory.Exists("TestResults"))
                Directory.Delete("TestResults", true);
        }

        [TestMethod]
        public void TestBackup() {
            Directory.CreateDirectory(@"TestResults\TestBackup\");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestBackup\packedFile.packed", true);
            File.Copy(@"TestData\examplefile1.txt", @"TestResults\TestBackup\examplefile1.txt", true);

            CheckRunCompareFile(new[] {"add", @"TestResults\TestBackup\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt",
                "--keepBackup"},
                @"TestResults\TestBackup\packedFile.packed.bak",
                @"TestData\empty.packed",
                "Backup after adding file to new"
            );

            CheckRunCompareFile(new[] {"add", @"TestResults\TestBackup\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt",
                "--keepBackup"},
                @"TestResults\TestBackup\packedFile.packed.bak",
                @"TestResults\TestBackup\packedFile.packed",
                "Backup after re-adding file"
            );

            CheckRunFileExists(new[] {"add", @"TestResults\TestBackup\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt" },
                @"TestResults\TestBackup\packedFile.packed.bak",
                "No backup because flag '--keepBackup' is not specified"
            );

            // todo: this test is not working because app even can't make backup. 
            //       Need to find way to open file/make folder with name "examplefile1.txt" 
            //       after making backup but before tying to extract.


            //File.Copy(@"TestData\examplefile3.txt", @"TestResults\TestBackup\examplefile1.txt", true);
            //var fsFile = new FileStream(@"TestResults\TestBackup\examplefile1.txt", FileMode.Open);
            //try
            //{
            //    CheckRunCompareFile(new[] {"extract", @"TestResults\TestBackup\packedFile.packed",
            //    "--destinationPath", @"TestResults\TestBackup\examplefile1.txt",
            //    "--packedPath", @"file.txt" },
            //    @"TestResults\TestBackup\examplefile1.txt",
            //    @"TestData\examplefile3.txt",
            //    "Restore backup of file after failng extraction", 1
            //    );
            //}
            //finally
            //{
            //    fsFile.Close();
            //}

            //NOTE: Becaouse of change in ScrapPackedFile.cs:469 this test is not correct.
            //CheckRunFileExists(new[] {"extract", @"TestResults\TestBackup\packedFile.packed",
            //    "--destinationPath", @"TestResults\TestBackup\examplefile1.txt",
            //    "--packedPath", @"file.txt",
            //    "--keepBackup"},
            //    @"TestResults\TestBackup\examplefile1.txt.bak",
            //    "Backup of alredy extracted file after extraction"
            //);

            // todo: overwriteOldBackup
            // Todo: implement check later
            /* // add file keep backup
            // (create dummy packedFile.packed.bak before call, expect to be unchanged)
            CheckRun(new[] { "add", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile1.txt'", "--packedPath", "'folder/file.txt'", "--keepBackup" }, "", "");
            // add file keep backup but overwrite old backup
            CheckRun(new[] { "add", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile3.txt'", "--packedPath", "'folder/file.txt'", "--keepBackup", "--overwriteOldBackup" }, "", "");
            // add file keep do not keep backup and old backup
            // (create dummy packedFile.packed.bak before call, expect to be unchanged)
            CheckRun(new[] { "add", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile2.png'", "--packedPath", "'folder/file.png'", "--overwriteOldBackup" }, "", "");
            */

            // todo: find a way to test MakeBackup(), RestoreBackup() and DeleteBackup()
        }

        [TestMethod]
        public void TestCat() {
            CheckRunCompareOutput(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "file1.txt"},
                "Begin of this first file in the example packed container.\r\n" +
                "Content not relevant. This is already the END\r\n",
                "Cat expample.packed file1.txt"
            );

            CheckRunCompareOutput(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "folder1/file2.png"},
                "�PNG\r\n\u001a\n\0\0\0\rIHDR\0\0\0\u0019\0\0\0\u0019\u0001\u0003\0\0\0�'\u0017 \0\0\0�z" +
                "TXtRaw profile type exif\0\0xڅP�\r�0\b�g���\u0001��q�T�\u0006\u001d�8���JA2p\u001c:\f�|" +
                "�/xtc\u0012��K�)��T��4)h�\\}@Y���\\\u0018�\u0013�%��\u0006+y�V\u000f����Y<\tU�\u001e�#�" +
                "6�r\u0011�\u0001�\u007f��م�\v1\u0019\u0011ķJ\u0016S-����\u0013(�2�6O8Z9\u001eM���W,Y�:" +
                "G��\u0004�p`TOL�3�/p�(��3)�����lv1=Զ6�'��~�x\u001c\u001f�w�X\u000f��\u0001�\u0013.��G" +
                "\u0016�\u000e�`�k��~|�\u0002�H��ṍ\v\0\0\0\u0006PLTE\0\0\0����ٟ�\0\0\0\u0001bKGD\0�\u0005" +
                "\u001dH\0\0\0\tpHYs\0\0.#\0\0.#\u0001x�?v\0\0\0\atIME\a�\u0004\u0001\r('7�i�\0\0\0}IDAT" +
                "\b�c`�~����)�����\"�X\u0002\"��Dm�}\u0006\u0006�P\a\u0006����\u001b\u0018t��40�^\u001d���" +
                "����a��v\u0006�'w�00L��!��r\u0003ê@�\u0006\u0006���@\u001dJ�\r\f��A@SZ\v��ei`p�v\a\u0012�V" +
                "\u0002żw\u0001M�2b\0\0Q�(�����\0\0\0\0IEND�B`�\r\n",
                "Cat expample.packed folder1/file2.png"
            );

            CheckRunCompareOutput(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "folder1/file2.png", "--asHex"},
                "00000000 8950 4E47 0D0A 1A0A 0000 000D 4948 4452 \r\n" +
                "00000010 0000 0019 0000 0019 0103 0000 00FE 2717 \r\n" +
                "00000020 2000 0000 EB7A 5458 7452 6177 2070 726F \r\n" +
                "00000030 6669 6C65 2074 7970 6520 6578 6966 0000 \r\n" +
                "00000040 78DA 8550 DB0D C330 08FC 678A 8E80 01BF \r\n" +
                "00000050 C671 9354 EA06 1DBF 3890 87D3 4A41 3270 \r\n" +
                "00000060 1C3A 0CB0 7CDE 2F78 7463 1290 984B AA29 \r\n" +
                "00000070 A19A 54A9 D434 2968 F65C 7D40 59BD 81EA \r\n" +
                "00000080 5C18 EBB0 13A4 25D6 C806 2B79 FF56 0FBB \r\n" +
                "00000090 8085 A659 3C09 559F 1E9E 23D1 36FD 7211 \r\n" +
                "000000A0 F201 DC7F D4F3 D985 9A0B 3119 11C4 B74A \r\n" +
                "000000B0 1653 2DF9 BCC2 E413 2896 32AC 364F 385A \r\n" +
                "000000C0 391E 4DDE E6CD 572C 59AF 3A47 9DCF 04B4 \r\n" +
                "000000D0 7060 544F 4CF6 33EE 2F70 D328 ABD7 3329 \r\n" +
                "000000E0 8E9A 8BA2 A66C 7631 3DD4 B636 9C27 ECC6 \r\n" +
                "000000F0 7E8B 781C 1F93 77C9 580F 8BD5 0193 132E \r\n" +
                "00000100 C6E9 4716 FF0E BB60 B86B B8C5 7E7C F802 \r\n" +
                "00000110 8D48 83B5 E1B9 8D0B 0000 0006 504C 5445 \r\n" +
                "00000120 0000 00FF FFFF A5D9 9FDD 0000 0001 624B \r\n" +
                "00000130 4744 0088 051D 4800 0000 0970 4859 7300 \r\n" +
                "00000140 002E 2300 002E 2301 78A5 3F76 0000 0007 \r\n" +
                "00000150 7449 4D45 07E5 0401 0D28 2737 EE69 A600 \r\n" +
                "00000160 0000 7D49 4441 5408 D763 60FC 7E80 81A1 \r\n" +
                "00000170 9629 9E81 C1D5 EB22 9058 0222 9480 446D \r\n" +
                "00000180 DC7D 0606 C650 0706 86FF E1FF 1B18 749E \r\n" +
                "00000190 BA34 30EC 5E1D D6C0 D0F9 AAB3 8161 D3E4 \r\n" +
                "000001A0 7606 8627 77DF 3030 4CDB F2A4 8121 C4F1 \r\n" +
                "000001B0 7203 C3AA 40FE 0606 85FD EC40 1D4A E50D \r\n" +
                "000001C0 0C8C 9641 4053 5A0B 80E6 AD65 6960 70F5 \r\n" +
                "000001D0 7607 12E7 5602 C5BC 7701 4D8E 3262 0000 \r\n" +
                "000001E0 51B6 28C8 EEF9 BDBA 0000 0000 4945 4E44 \r\n" +
                "000001F0 AE42 6082 \r\n",
                "Cat expample.packed folder1/file2.png as hex"
            );

            CheckRunCompareOutput(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "file2.txt", "--asHex", "-g", "2", "-r", "20", "-f", "d3", "-l", "d3"},
                "000 066101 103105 110032 111102 032116 104101 032115 101099 111110 100032 \r\n" +
                "020 102105 108101 032105 110032 116104 101032 101120 097109 112108 101032 \r\n" +
                "040 112097 099107 101100 032099 111110 116097 105110 101114 046013 010065 \r\n" +
                "060 103097 105110 032116 104101 032099 111110 116101 110116 032105 115032 \r\n" +
                "080 110111 116032 114101 108101 118097 110116 046013 010070 111114 032101 \r\n" +
                "100 097115 105101 114032 097110 097121 122101 032097 108108 032116 101120 \r\n" +
                "120 116102 105108 101115 032115 116097 114116 032119 105116 104032 066032 \r\n" +
                "140 101032 103032 105032 110032 097110 100032 101032 110032 100032 119105 \r\n" +
                "160 116104 032046 046046 013010 069078 068\r\n",
                "Cat expample.packed file2.txt as hex with modificators"
            );

            CheckRunCompareOutput(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "file2.txt", "--asHex", "--noPrintLinesNumbers"},
                "4265 6769 6E20 6F66 2074 6865 2073 6563 \r\n" +
                "6F6E 6420 6669 6C65 2069 6E20 7468 6520 \r\n" +
                "6578 616D 706C 6520 7061 636B 6564 2063 \r\n" +
                "6F6E 7461 696E 6572 2E0D 0A41 6761 696E \r\n" +
                "2074 6865 2063 6F6E 7465 6E74 2069 7320 \r\n" +
                "6E6F 7420 7265 6C65 7661 6E74 2E0D 0A46 \r\n" +
                "6F72 2065 6173 6965 7220 616E 6179 7A65 \r\n" +
                "2061 6C6C 2074 6578 7466 696C 6573 2073 \r\n" +
                "7461 7274 2077 6974 6820 4220 6520 6720 \r\n" +
                "6920 6E20 616E 6420 6520 6E20 6420 7769 \r\n" +
                "7468 202E 2E2E 0D0A 454E 44\r\n",
                "Cat expample.packed file2.txt as hex no print line numbers"
            );
        }

        [TestMethod]
        public void TestCatFailed() {
            CheckRunFail(new[] {"cat", @"TestData\example.packed",
                "--packedPath", "nonexsists.txt"},
                1,
                "Test cat falied"
            );
        }

        // Comapators
        private static void CheckRunCompareFile(string[] p_Args, string p_ExpectedFilePath, string p_ActualFilePath, string p_Message = "", int p_ReturnCode = 0) {
            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(p_ReturnCode, returnValue, p_Message + ": wrong return value");
            AssertFilesEqual(p_ExpectedFilePath, p_ActualFilePath, p_Message + ": files differ");
        }

        // todo: remove this method?
        private static void CheckRunFail(string[] p_Args, int p_ExpectedReturnValue, string p_Message = "") {
            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(p_ExpectedReturnValue, returnValue, p_Message + ": wrong return value");

            // todo make check for console output?
            // todo check files not modified?
        }

        private static void CheckRunCompareFolder(string[] p_Args, string p_ExpectedFolderPath, string p_ActualFolderPath, string p_Message = "") {
            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message + ": wrong return value");
            AssertFoldersEqual(p_ExpectedFolderPath, p_ActualFolderPath, p_Message + ": folders differ");
        }

        private static void CheckRunCompareOutput(string[] p_Args, string p_ExpectedOutput, string p_Message = "") {
            StringWriter stringWriter = new();
            Console.SetOut(stringWriter);

            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message + ": wrong return value");

            string ActualOutput = stringWriter.ToString();
            Assert.AreEqual("\r\n" + p_ExpectedOutput, "\r\n" + ActualOutput, p_Message + ": outputs are not equal");
        }
        private static void CheckRunCompareErrorOutput(string[] p_Args, string p_ExpectedOutput, string p_Message = "") {
            StringWriter stringWriter = new();
            Console.SetError(stringWriter);

            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(1, returnValue, p_Message + ": wrong return value");

            string ActualOutput = stringWriter.ToString();
            Assert.AreEqual("\r\n" + p_ExpectedOutput, "\r\n" + ActualOutput, p_Message + ": outputs are not equal");
        }

        // TODO :CheckRunFile**Not**Exsits?
        private static void CheckRunFileExists(string[] p_Args, string p_UnexpectedOutput, string p_Message) {
            int returnValue = CliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message + ": wrong return value");
            Assert.IsTrue(!File.Exists(p_UnexpectedOutput), p_Message + ": file exists but should not");
        }

        // Asserts
        private static void AssertFilesEqual(string p_FileExpected, string p_FileActual, string p_Message = "") {
            Assert.IsTrue(FilesEqual(p_FileExpected, p_FileActual), p_Message);
        }

        private static void AssertFoldersEqual(string p_FolderExpected, string p_FolderActual, string p_Message = "") {
            Assert.IsTrue(Directory.Exists(p_FolderExpected), p_Message + ": expected folder missing");
            Assert.IsTrue(Directory.Exists(p_FolderActual), p_Message + ": actual folder missing");

            List<string> expectedFiles = new(Directory.GetFiles(p_FolderExpected, "", SearchOption.AllDirectories));
            List<string> actualFiles = new(Directory.GetFiles(p_FolderActual, "", SearchOption.AllDirectories));

            Assert.AreEqual(expectedFiles.Count, actualFiles.Count, p_Message + ": different amount of files");

            expectedFiles.Sort();
            actualFiles.Sort();

            for (int i = 0; i < expectedFiles.Count; i++) {
                AssertFilesEqual(expectedFiles[i], actualFiles[i], p_Message + ": comparing " + expectedFiles[i] + " = " + actualFiles[i]);
            }
        }

        private static bool FilesEqual(string p_FileA, string p_FileB) {
            byte[] fileA = File.ReadAllBytes(p_FileA);
            byte[] fileB = File.ReadAllBytes(p_FileB);
            if (fileA.Length == fileB.Length) {
                for (int i = 0; i < fileA.Length; i++) {
                    if (fileA[i] != fileB[i]) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
