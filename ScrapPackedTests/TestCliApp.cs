using ch.romibi.Scrap.Packed.Explorer.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

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
                "--packedPath", "folder/file.txt",
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
            CheckRunCompareOutput(new[] { "list", @"TestData\empty.packed" },
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

            CheckRunCompareOutput(new[] { "list", @"TestData\example.packed",
                "--searchString", "nothing"},
                "Could not find anything by query 'nothing' in 'TestData\\example.packed'\r\n",
                "List could not find"
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
                "--packedPath", @"file.txt" },
                @"TestResults\TestBackup\packedFile.packed.bak",
                @"TestData\empty.packed",
                "Backup after adding file to new"
            );

            CheckRunCompareFile(new[] {"add", @"TestResults\TestBackup\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt" },
                @"TestResults\TestBackup\packedFile.packed.bak",
                @"TestResults\TestBackup\packedFile.packed",
                "Backup after re-adding file"
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

            CheckRunFileExists(new[] {"extract", @"TestResults\TestBackup\packedFile.packed",
                "--destinationPath", @"TestResults\TestBackup\examplefile1.txt",
                "--packedPath", @"file.txt" },
                @"TestResults\TestBackup\examplefile1.txt.bak",
                "Backup of alredy extracted file after extraction"
            );

            // todo: keepBackup & overwriteOldBackup
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
