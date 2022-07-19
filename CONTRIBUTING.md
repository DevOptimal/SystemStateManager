# Contributing

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT.md)

This project has adopted the [code of conduct](CODE_OF_CONDUCT.md) as defined by the [Contributor Convenant organization](https://www.contributor-covenant.org/). Please take time to review it before contributing to this project.

## Prerequisites

- _Required_: [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet) with a [version that matches](https://docs.microsoft.com/en-us/dotnet/core/tools/global-json) the [global.json](global.json) file. If in doubt, just install the version specified in the `$.sdk.version` field. Ensure that installation directory gets added to your `PATH` environment variable.
- _Recommended_: [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (Windows only)

## Build

### Console

1. Open your favorite console
1. Navigate to the root of your local clone of this repository
1. Execute the following command:
    ```
    dotnet build
    ```

### Visual Studio

1. Start Visual Studio
1. Click `Open a project or solution`
1. Navigate to the root of your local clone of this repository
1. Select `SystemStateManager.sln` and click `Open`
1. Under the `Build` menu, click `Build Solution`

## Test

### Console

1. Open your favorite console
1. Navigate to the root of your local clone of this repository
1. Execute the following command:
    ```
    dotnet test
    ```

### Visual Studio

1. Start Visual Studio
1. Click `Open a project or solution`
1. Navigate to the root of your local clone of this repository
1. Select `SystemStateManager.sln` and click `Open`
1. Under the `Test` menu, click `Run All Tests`

## Pull Requests

Pull requests welcome!

This project adheres to the principles of [test-driven development](https://en.wikipedia.org/wiki/Test-driven_development). All pull requests should include one or more tests that demonstrate a bug or missing feature. They may also be paired with changes that fix/add the corresponding bug/feature.

All tests in `main` should always pass. If you are contributing a test that fails (e.g. to demostrate a bug), you must mark it with one of the following test categories:
| Failure Reason | Test Category |
| -------------- | ------------- |
| Demonstrates a bug | `Bug` |
| Demonstrates a mising feature | `MissingFeature` |

For example, to mark a test that demonstrates a bug in [MSTest](https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=mstest), use the [`TestCategory` attribute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.testcategoryattribute):
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestNamespace
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod, TestCategory("Bug")]
        public void TestMethod1()
        {
        }
    }
}
```
