using ch.romibi.Scrap.Packed.Explorer;
using ch.romibi.Scrap.Packed.Explorer.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace ch.romibi.Scrap.Packed.PackerLib.Tests
{
    [TestClass]
    public class TestCliApp
    {
        // Note: if some tests fail for no reason cleanup TestData folder in the output folder
        // Todo: ensure that this is not needed

        [TestInitialize]
        public void TestInitialize()
        {
            if(Directory.Exists("TestResults"))
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


        [TestMethod]
        public void TestRunAddFile()
        {
            Directory.CreateDirectory(@"TestResults\TestAdd");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestAdd\packedFile.packed", true);

            // add file new
            CheckRunCompareFile(new[] {"add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", @"file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileNew.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to new"
            );

            // add file existing to root
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExistingToRoot.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing to root"
            );

            // add file existing
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder/file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExisting.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing");

            // add file replace
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed", 
                "--sourcePath", @"TestData\examplefile3.txt",
                "--packedPath", "folder/file.txt" },
                @"TestData\TestReferenceFiles\TestAdd\addFileReplace.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add file to existing and replace");

            // add file different output
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder/file.txt",
                "--outputPackedFile", @"TestResults\TestAdd\newPackedFile.packed" },
                @"TestData\TestReferenceFiles\TestAdd\addFileExisting.packed",
                @"TestResults\TestAdd\newPackedFile.packed",
                "Store to different output");
            AssertFilesEqual(@"TestData\TestReferenceFiles\TestAdd\addFileReplace.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Store to different output: previous file modified");

            // Todo: implement check later
            /* // add file keep backup
            // (create dummy packedFile.packed.bak before call, expect to be unchanged)
            CheckRun(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile1.txt'", "--packedPath", "'folder/file.txt'", "--keepBackup" }, "", "");
            // add file keep backup but overwrite old backup
            CheckRun(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile3.txt'", "--packedPath", "'folder/file.txt'", "--keepBackup", "--overwriteOldBackup" }, "", "");
            // add file keep do not keep backup and old backup
            // (create dummy packedFile.packed.bak before call, expect to be unchanged)
            CheckRun(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed", "--sourcePath", "'examplefile2.png'", "--packedPath", "'folder/file.png'", "--overwriteOldBackup" }, "", "");
            */

            if (Directory.Exists("TestResults"))
                Directory.Delete("TestResults", true);
        }

        [TestMethod]
        public void TestRunAddFolder()
        {
            Directory.CreateDirectory(@"TestResults\TestAdd");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestAdd\packedFile.packed", true);

            // add folder new
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder1\" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderNew.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to new");

            // add folder existing root
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder2\" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderExistingToRoot.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to root");

            // add folder existing subfolder
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed", 
                "--sourcePath", @"TestData\exampleFolder1\",
                "--packedPath", "subfolder/" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderExistingToSubfolder.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to subfolder");

            // add folder replace some
            CheckRunCompareFile(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFolder2\",
                "--packedPath", "subfolder/" },
                @"TestData\TestReferenceFiles\TestAdd\addFolderReplaceSome.packed",
                @"TestResults\TestAdd\packedFile.packed",
                "Add folder to existing to subfolder, replace some");

            // Note: keepBackup & overwriteOldBackup tested via add file

            if (Directory.Exists("TestResults"))
                Directory.Delete("TestResults", true);
        }

        [TestMethod]
        public void TestRunAddFailed()
        {
            // add file missing
            CheckRunFail(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", "exampleFile_missing.txt",
                "--packedPath", "file.txt"},
                1, "Expected file not found");

            // add file, file not readable
            // todo test add file that is not readable
            /* CheckRunFail(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", "examplefile_readprotected.txt",
                "--packedPath", "file.txt"},
                1, "expected file not accessible"); */


            // add folder, folder not found
            CheckRunFail(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", "exampleFolder_missing/",
                "--packedPath", "subfolder/"},
                1, "Expected file not found");

            // add folder, some files not readable
            // todo test where some files in folder to ad are not readable
            /* CheckRunFail(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", "exampleFolder_readprotected/",
                "--packedPath", "subfolder/"},
                1, "expected some file not found"); */
        }

        [TestMethod]
        public void TestRunRemove()
        {
            Directory.CreateDirectory(@"TestResults\TestRemove");
            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);

            CheckRunCompareFile(new[] { "remove", "--packedFile" , @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "file1.txt"},
                @"TestData\TestReferenceFiles\TestRemove\removedFile.packed",
                @"TestResults\TestRemove\packedFile.packed",
                "Remove file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);
            CheckRunCompareFile(new[] { "remove", "--packedFile" , @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "folder1/"},
                @"TestData\TestReferenceFiles\TestRemove\removedFolder.packed",
                @"TestResults\TestRemove\packedFile.packed",
                "Remove folder");

            // Note: outputPackedFile, keepBackup & overwriteOldBackup tested via add

            // Note: remove root does not work and will not be made to work, just create a new packed
            // Todo: Check correctness of statement above
        }

        [TestMethod]
        public void TestRunRemoveFailed()
        {
            Directory.CreateDirectory(@"TestResults\TestRemove");
            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);

            CheckRunFail(new[] { "remove", "--packedFile" , @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "file_missing.txt"},
                1, "Remove file does not exist");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRemove\packedFile.packed", true);
            CheckRunFail(new[] { "remove", "--packedFile" , @"TestResults\TestRemove\packedFile.packed",
                "--packedPath", "folder_missing/"},
                1, "Remove folder does not exist");
        }

        [TestMethod]
        public void TestRunRename()
        {
            Directory.CreateDirectory(@"TestResults\TestRename");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", "--packedFile" , @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "file1.txt",
                "--newPackedPath", "file_renamed.txt"},
                @"TestData\TestReferenceFiles\TestRename\renameFile.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", "--packedFile" , @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "folder1/",
                "--newPackedPath", "directory/"},
                @"TestData\TestReferenceFiles\TestRename\renameFolder.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename folder");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunCompareFile(new[] { "rename", "--packedFile" , @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "/",
                "--newPackedPath", "sub/"},
                @"TestData\TestReferenceFiles\TestRename\renameRoot.packed",
                @"TestResults\TestRename\packedFile.packed",
                "Rename root");
        }

        [TestMethod]
        public void TestRunRenameFailed()
        {
            Directory.CreateDirectory(@"TestResults\TestRename");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunFail(new[] { "rename", "--packedFile" , @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "file_missing.txt",
                "--newPackedPath", "file_renamed.txt"},
                1, "Rename missing file");

            File.Copy(@"TestData\example.packed", @"TestResults\TestRename\packedFile.packed", true);
            CheckRunFail(new[] { "rename", "--packedFile" , @"TestResults\TestRename\packedFile.packed",
                "--oldPackedPath", "folder_missing/",
                "--newPackedPath", "directory/"},
                1, "Rename missing folder");

            // Note: outputPackedFile, keepBackup & overwriteOldBackup tested via add
        }

        [TestMethod]
        public void TestRunExtract()
        {
            CheckRunCompareFile(new[] { "extract", "--packedFile", @"TestData\example.packed",
                "--packedPath", "file1.txt",
                "--destinationPath", @"TestResults\TestExtract\file.txt"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractFile\file.txt",
                @"TestResults\TestExtract\file.txt",
                "Extract file");

            CheckRunCompareFolder(new[] { "extract", "--packedFile", @"TestData\example.packed",
                "--packedPath", "folder1/",
                "--destinationPath", @"TestResults\TestExtract\someFolder\"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractFolder\someFolder\",
                @"TestResults\TestExtract\someFolder\",
                "Extract folder");

            CheckRunCompareFolder(new[] { "extract", "--packedFile", @"TestData\example.packed",
                "--destinationPath", @"TestResults\TestExtract\all\"},
                @"TestData\TestReferenceFiles\TestExtract\ExtractAll\",
                @"TestResults\TestExtract\all\",
                "Extract all");
        }

        [TestMethod]
        public void TestRunExtractFailed()
        {
            Assert.Fail("check not implemented");
            // todo: think about failed extract calls
        }

        [TestMethod]
        public void TestRunList()
        {
            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed" },
                "file1.txt\n" +
                "file2.txt\n" +
                "folder1/file1.txt\n" +
                "folder1/file2.png\n" +
                "folder2/file1.txt\n" +
                "folder2/file2.txt\n" +
                "folder2/folder1/file1.txt\n" +
                "folder2/folder1/file2.txt",
                "List");

            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed",
                "--searchString", ".txt" },
                "file1.txt\n" +
                "file2.txt\n" +
                "folder1/file1.txt\n" +
                "folder2/file1.txt\n" +
                "folder2/file2.txt\n" +
                "folder2/folder1/file1.txt\n" +
                "folder2/folder1/file2.txt",
                "List .txt");

            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed",
                "--searchString", "folder2/*.txt" },
                "folder2/file1.txt\n" +
                "folder2/file2.txt\n" +
                "folder2/folder1/file1.txt\n" +
                "folder2/folder1/file2.txt",
                "List folder2/*.txt");

            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed",
                "--searchString", @"folder2/.*\.txt", "--regex" },
                "folder2/file1.txt\n" +
                "folder2/file2.txt\n" +
                "folder2/folder1/file1.txt\n" +
                "folder2/folder1/file2.txt",
                "List folder2/.*\\.txt");

            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed",
                "--outputStyle", "tree"},
                "│   file1.txt\n" +
                "│   file2.txt\n" +
                "│\n" +
                "├───folder1\n" +
                "│       file1.txt\n" +
                "│       file2.png\n" +
                "│\n" +
                "└───folder2\n" +
                "    │   file1.txt\n" +
                "    │   file2.txt\n" +
                "    │\n" +
                "    └───folder1\n" +
                "            file1.txt\n" +
                "            file2.txt",
                "List as tree");

            CheckRunCompareOutput(new[] { "list", "--packedFile", @"TestData\example.packed",
                "--outputStyle", "names",
                "--searchString", "folder2/" },
            "file1.txt\n" +
            "file2.txt\n" +
            "file1.txt\n" +
            "file2.txt",
            "List files with only filename from folder2");
        }

        [TestMethod]
        public void TestRunListFailed()
        {
            Assert.Fail("check not implemented");
            // todo: think about failed list calls
        }

        [TestMethod]
        public void TestInputPackedFail()
        {
            // check uncorrect input
            CheckRunFail(new[] {"add", "--packedFile", "/.,*&^$Q*",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt"}, 1, "expected file is nonexists");

            // check nonexisted output
            CheckRunFail(new[] {"add", "--packedFile", "nonexsited.packed",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt"}, 1, "expected file is nonexists");

            if (!Directory.Exists(@"TestResults\TestAdd"))
                Directory.CreateDirectory(@"TestResults\TestAdd");
            if (File.Exists(@"TestResults\TestAdd\packedFile.packed"))
                File.Delete(@"TestResults\TestAdd\packedFile.packed");

            // check inaccessable packed
            var fsFile = new FileStream(@"TestResults\TestAdd\packedFile.packed", FileMode.OpenOrCreate);
            try
            {
                CheckRunFail(new[] {"add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt"}, 1, "Expected file to be inaccessible");
            }
            finally
            {
                byte[] someContent = new[] { (byte)'H', (byte)'i' };
                fsFile.Write(someContent);
                fsFile.Close();
            }

            // check unreadable/invalid packed
            CheckRunFail(new[] {"add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\exampleFile1.txt",
                "--packedPath", "file.txt"}, 1, "Expected packed file to be invalid");

            if (File.Exists(@"TestResults\TestAdd\packedFile.packed"))
                File.Delete(@"TestResults\TestAdd\packedFile.packed");
        }

        [TestMethod]
        public void TestOutputPackedFail()
        {
            Directory.CreateDirectory(@"TestResults\TestAdd");
            File.Copy(@"TestData\empty.packed", @"TestResults\TestAdd\packedFile.packed", true);


            // Access denied
            Directory.CreateDirectory(@"TestResults\TestAdd\filenameWasTaken");
            CheckRunFail(new[] { "add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                "--sourcePath", @"TestData\examplefile1.txt",
                "--packedPath", "folder/file.txt",
                "--outputPackedFile", @"TestResults\TestAdd\filenameWasTaken" },
                1, "Access to output file is denied");

            Directory.Delete(@"TestResults\TestAdd\filenameWasTaken");

            // check inaccessable output packed
            var fsFile = new FileStream(@"TestResults\TestAdd\packedOutFile.packed", FileMode.OpenOrCreate);
            try
            {
                CheckRunFail(new[] {"add", "--packedFile", @"TestResults\TestAdd\packedFile.packed",
                    "--sourcePath", @"TestData\examplefile1.txt",
                    "--packedPath", "file.txt",
                    "--outputPackedFile", @"TestResults\TestAdd\packedOutFile.packed" },
                    1, "Expected output file to be inaccessible");
            }
            finally
            {
                fsFile.Close();
            }

            if (Directory.Exists("TestResults"))
                Directory.Delete("TestResults", true);
        }

        private void CheckRunCompareFile(string[] p_Args, string p_ExpectedFilePath, string p_ActualFilePath, string p_Message = "")
        {
            var cliApp = new CliApp();
            var returnValue = cliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message+": wrong return value");
            AssertFilesEqual(p_ExpectedFilePath, p_ActualFilePath, p_Message + ": files differ");
        }

        private void CheckRunFail(string[] p_Args, int p_ExpectedReturnValue, string p_Message = "")
        {
            var cliApp = new CliApp();
            var returnValue = cliApp.Run(p_Args);

            Assert.AreEqual(p_ExpectedReturnValue, returnValue, p_Message + ": wrong return value");

            // todo make check for console output?
            // todo check files not modified?
        }

        private void CheckRunCompareFolder(string[] p_Args, string p_ExpectedFolderPath, string p_ActualFolderPath, string p_Message = "")
        {
            var cliApp = new CliApp();
            var returnValue = cliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message + ": wrong return value");
            AssertFoldersEqual(p_ExpectedFolderPath, p_ActualFolderPath, p_Message + ": folders differ");
        }

        private void CheckRunCompareOutput(string[] p_Args, string p_ExpectedOutput, string p_Message="")
        {
            var cliApp = new CliApp();
            var returnValue = cliApp.Run(p_Args);

            Assert.AreEqual(0, returnValue, p_Message + ": wrong return value");
            Assert.Fail("asserting output not yet implemented");
        }

        private void AssertFilesEqual(string p_FileExpected, string p_FileActual, string p_Message = "")
        {
            Assert.IsTrue(FilesEqual(p_FileExpected, p_FileActual), p_Message);
        }

        private void AssertFoldersEqual(string p_FolderExpected, string p_FolderActual, string p_Message = "")
        {
            Assert.IsTrue(Directory.Exists(p_FolderExpected), p_Message + ": expected folder missing");
            Assert.IsTrue(Directory.Exists(p_FolderActual), p_Message + ": actual folder missing");

            var expectedFiles = new List<string>(Directory.GetFiles(p_FolderExpected, "", SearchOption.AllDirectories));
            var actualFiles = new List<string>(Directory.GetFiles(p_FolderExpected, "", SearchOption.AllDirectories));

            Assert.AreEqual(expectedFiles.Count, actualFiles.Count, p_Message + ": different amount of files");

            expectedFiles.Sort();
            actualFiles.Sort();

            for (int i = 0; i < expectedFiles.Count; i++)
            {
                AssertFilesEqual(expectedFiles[i], actualFiles[i], p_Message + ": comparing " + expectedFiles[i] + " = " + actualFiles[i]);
            }
        }

        private bool FilesEqual(string p_FileA, string p_FileB)
        {
            byte[] fileA = File.ReadAllBytes(p_FileA);
            byte[] fileB = File.ReadAllBytes(p_FileB);
            if (fileA.Length == fileB.Length)
            {
                for (int i = 0; i < fileA.Length; i++)
                {
                    if (fileA[i] != fileB[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
