Contribute to Scrap Packed Explorer
=====================
We love your input! We want to make contributing to this project as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## We Develop with Github
We use github to host code, to track issues and feature requests, as well as accept pull requests.

## We Use [Github Flow][githubflow], So All Code Changes Happen Through Pull Requests
Pull requests are the best way to propose changes to the codebase (we use [Github Flow][githubflow]). We actively welcome your pull requests:

1. Fork the repo and create your branch from `master`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update the documentation.
4. Ensure the test suite passes.
5. Issue that pull request!

## Any contributions you make will be under the MIT Software License
In short, when you submit code changes, your submissions are understood to be under the same [MIT License](http://choosealicense.com/licenses/mit/) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using Github's [issues](https://github.com/romibi/Scrap-Packed-Explorer/issues)
We use GitHub issues to track public bugs. Report a bug by [opening a new issue](https://github.com/romibi/Scrap-Packed-Explorer/issues/new); it's that easy!

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

People *love* thorough bug reports. I'm not even kidding.

## Use a Consistent Coding Style

### Naming Conventions
* Every class and method should be named using **PascalCase** convention (example: `void ReadFile()`)
* Every method argument should be named using **PascalCase** convetion with prefix _p__ (example: `string p_FilePath`)
* Every variable should be named using **camelCase** convention (example: `int maxLength`)
* Every application's command line argument should be named using **camelCase** convention (example: `--keepBackup`)

### Brackets and indentation
Indentation must be done with 4 spaces. Lines should be ended with `\r\n` (Windows style)

Opening brackets should **always** be on the same line with the keyword that opens a block. If brackets **can** be removed - they **should** be removed.

`else`, `catch` and `finally` blocks should be on the same line as the previous closing bracket
For control-flow blocks (`if`, `while`, `for`, etc.) the brackets containing the condition should be separated from keyword with a space.

**Example:**
```c#
try {
    if (a == b) {
        // Some code...
        // Some code...
        // Some code...
    } else
        // One line of code
} catch {
    // Error handling code
}
```

### Class methods and properties arrangements
Methods and properties in every class should be in this order:

- public const/read-only properties
- public variables/non-const properties
- public methods
- protected (same sub order as public)
- private (same sub order as public)

All mehods should be grouped by their functuality.

**Example:**
```c#
Class SomeClass
{
    public string classProperty;

    SomeClass() {...}

    // Files group
    public File OpenFile(string p_FilePath) {...}
    public void WriteFile(File p_File, string p_Message) {...}
    public string ReadFile(File p_File) {...}

    // Console
    public void PrintMessage(string p_Message) {...}
    
    // Helper functions
    private void GetFileProperties(File p_File) {...}
    // ......Private methods and properties........
    // ............................................
}
```

## License
By contributing, you agree that your contributions will be licensed under its MIT License.

## References
This document was adapted from the open-source [basic template for contribution guidelines](https://gist.github.com/briandk/3d2e8b3ec8daf5a27a62)

[githubflow]: https://docs.github.com/en/get-started/quickstart/github-flow