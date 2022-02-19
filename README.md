# CppEmbeddedHeaderGenerator

 Generates a simple C++ header with `const std::string` for ASCII files and `const unsigned char` array for binary files.

## Usage

By default files will be treated as binary data and be stored as a `const unsigned char` array. If you want to be able to access the contents of the file as a `std::string`, then you may prefix the file name with `ascii_` (e.g. `subdir/example.txt` -> `subdir/ascii_example.txt`). Other encodings for text files are currently not supported.

When you are ready, you may run the executable to generate header files:

| <div style="width:20em">Argument</div> | Description
| - | - |
| `-e, --embedded-dir` | Required. Path to the directory containing the files to embed. |
| `-i, --ignorefile` | Path to the ignore file (in the format of a .gitignore). By default ".embedignore" in the same directory as the files to embed. |
| `-o, --output-dir` | Path to the output directory. By default current directory. |
| `--help` | Display this help screen. |
| `--version` | Display version information. |

The generated headers (`embedded.h` and `embedded-extractor.h`) will have the resources and the `extractAll(std::string outputDir)` function under the `embedded` namespace.

## Example

Suppose we have 2 file to embed:

- `LoremIpsum.txt`
- `Hello World.exe`

Rename the `LoremIpsum.txt` file to `ascii_LoremIpsum.txt`:

- `ascii_LoremIpsum.txt`
- `Hello World.exe`

If there are other files in the same directory as the 2 above (or in any of it's sub-directories), create a `.embedignore` file with the appropriate rules (similar to .gitignore files).

Run the executable with the necessary arguments:

`CppEmbeddedHeaderGenerator -e "path/to/source/directory" -o "path/to/output/directory"`

The result will be:

```cpp
// embedded.h
#ifndef EMBEDDED_RESOURCES_HEADER_FILE
#define EMBEDDED_RESOURCES_HEADER_FILE

#include <string>

namespace embedded
{

    std::string empty = "";
    extern __declspec(selectany) std::string LoremIpsum_txt_name = "Lorem Ipsum.txt";
    extern __declspec(selectany) std::string LoremIpsum_txt = empty
        + "Lorem ipsum dolor sit amet, ..."
        ;
    extern __declspec(selectany) std::string Hello_World_exe_name = "Hello World.exe";
    extern __declspec(selectany) int Hello_World_exe_size = 15872;
    extern __declspec(selectany) unsigned char Hello_World_exe[15872] = { 77, 90, 144, 0, 3, 0, 0, 0, 4, ... };

}

#endif
```

```cpp
// embedded-extractor.h
#ifndef EMBEDDED_RESOURCES_EXTRACTOR_HEADER_FILE
#define EMBEDDED_RESOURCES_EXTRACTOR_HEADER_FILE

#include "embedded.h"
#include <iostream>
#include <fstream>
#include <string>
#include <filesystem>

#include <string>

namespace embedded
{

    bool _getDirectory(std::string const& filePath, std::string& directoryPath)
    {
        std::string::size_type pos = filePath.find_last_of('/');
        if (pos != std::string::npos)
        {
            directoryPath = filePath.substr(0, pos);
            return true;
        }
        return false;
    }

    void extractAll(std::string const outputDir = ".", bool verbose = false)
    {
        if (outputDir != ".")
        {
            if (verbose) std::cout << "Creating the \"" << outputDir << "\" directory." << std::endl;
            std::filesystem::create_directory(outputDir);
        }

        std::string dirPath;
        std::ofstream file;

        if (verbose) std::cout << "Extracting the \"" << embedded::LoremIpsum_txt_name << "\" resource file." << std::endl;
        if (_getDirectory(embedded::LoremIpsum_txt_name, dirPath))
        {
            dirPath = outputDir + "/" + dirPath;
            if (verbose) std::cout << "Creating the \"" << dirPath << "\" directory." << std::endl;
            std::filesystem::create_directory(dirPath);
        }
        file.open(outputDir + "/" + embedded::LoremIpsum_txt_name);
        file << embedded::LoremIpsum_txt;
        file.close();

        if (verbose) std::cout << "Extracting the \"" << embedded::Hello_World_exe_name << "\" resource file." << std::endl;
        if (_getDirectory(embedded::Hello_World_exe_name, dirPath))
        {
            dirPath = outputDir + "/" + dirPath;
            if (verbose) std::cout << "Creating the \"" << dirPath << "\" directory." << std::endl;
            std::filesystem::create_directory(dirPath);
        }
        file.open(outputDir + "/" + embedded::Hello_World_exe_name, std::ios::out | std::ios::binary);
        file.write((char*)&embedded::Hello_World_exe[0], embedded::Hello_World_exe_size);
        file.close();

    }

}

#endif
```

## MIT License

Copyright (c) 2021 Guiorgy

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
