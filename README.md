# CppEmbeddedHeaderGenerator

 Generates a simple C++ header with `const std::string_view` for ASCII files and `const char` array for binary files.

## Usage

By default files will be treated as binary data and be stored as a `const char` array. If you want to be able to access the contents of the file as a `std::string_view`, then you may prefix the file name with `ascii_` (e.g. `subdir/example.txt` -> `subdir/ascii_example.txt`). Other encodings for text files are currently not supported.

When you are ready, you may run the executable to generate header files:

| <div style="width:20em">Argument</div> | Description
| - | - |
| `-e, --embedded-dir` | Required. Path to the directory containing the files to embed. |
| `-i, --ignorefile` | Path to the ignore file (in the format of a .gitignore). By default ".embedignore" in the same directory as the files to embed. |
| `-o, --output-dir` | Path to the output directory. By default current directory. |
| `--help` | Display this help screen. |
| `--version` | Display version information. |

The generated headers (`embedded.h` and `embedded-extractor.h`) will have the resources and the `extractAll(std::string outputDir)` function, as well as `extract[FILE NAME](std::string outputDir)` functions under the `embedded` namespace.

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

    extern __declspec(selectany) constexpr std::string_view LoremIpsum_txt_name = std::string_view("LoremIpsum.txt");
    extern __declspec(selectany) constexpr int LoremIpsum_txt__ascii_chunks = 3;
    extern __declspec(selectany) constexpr std::string_view LoremIpsum_txt__ascii_chunk_0 = std::string_view("\n\nLorem ipsum dolor...");
    extern __declspec(selectany) constexpr std::string_view LoremIpsum_txt__ascii_chunk_1 = std::string_view("Suspendisse condimentum cursus...");
    extern __declspec(selectany) constexpr std::string_view LoremIpsum_txt__ascii_chunk_2 = std::string_view("Vivamus sodales fringilla...");
    extern __declspec(selectany) constexpr std::string_view bin_exe_name = std::string_view("bin.exe");
    extern __declspec(selectany) constexpr int bin_exe__blob_chunks = 3;
    extern __declspec(selectany) constexpr int bin_exe_size_0 = 16301;
    extern __declspec(selectany) constexpr char bin_exe__blob_chunk_0[16302] = "MZ\x90\0\x3\0\0\0\x4\0\0\0\xFF...";
    extern __declspec(selectany) constexpr int bin_exe_size_1 = 16301;
    extern __declspec(selectany) constexpr char bin_exe__blob_chunk_1[16302] = "\x8B\xFBH\x8B\xD6H\xFM\xF8H...";
    extern __declspec(selectany) constexpr int bin_exe_size_2 = 16301;
    extern __declspec(selectany) constexpr char bin_exe__blob_chunk_2[16302] = "\x18I\x89s WATAUAVAWH\x81\xEC..."

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
        if (std::string::size_type pos = filePath.find_last_of('/'); pos != std::string::npos)
        {
            directoryPath = filePath.substr(0, pos);
            return true;
        }
        return false;
    }

    void extract_LoremIpsum_txt(std::string const outputDir = ".", bool verbose = false)
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
        file.open(outputDir + "/" + embedded::LoremIpsum_txt_name.data());
        file << embedded::LoremIpsum_txt__ascii_chunk_0;
        file << embedded::LoremIpsum_txt__ascii_chunk_1;
        file << embedded::LoremIpsum_txt__ascii_chunk_2;
        file.close();
    }

    void extract_bin_exe(std::string const outputDir = ".", bool verbose = false)
    {
        if (outputDir != ".")
        {
            if (verbose) std::cout << "Creating the \"" << outputDir << "\" directory." << std::endl;
            std::filesystem::create_directory(outputDir);
        }

        std::string dirPath;
        std::ofstream file;

        if (verbose) std::cout << "Extracting the \"" << embedded::bin_exe_name << "\" resource file." << std::endl;
        if (_getDirectory(embedded::bin_exe_name, dirPath))
        {
            dirPath = outputDir + "/" + dirPath;
            if (verbose) std::cout << "Creating the \"" << dirPath << "\" directory." << std::endl;
            std::filesystem::create_directory(dirPath);
        }
        file.open(outputDir + "/" + embedded::bin_exe_name.data(), std::ios::out | std::ios::binary);
        file.write(&embedded::bin_exe__blob_chunk_0[0], embedded::bin_exe_size_0);
        file.write(&embedded::bin_exe__blob_chunk_1[0], embedded::bin_exe_size_1);
        file.write(&embedded::bin_exe__blob_chunk_2[0], embedded::bin_exe_size_2);
        file.close();
    }

    void extractAll(std::string const outputDir = ".", bool verbose = false)
    {
        if (outputDir != ".")
        {
            if (verbose) std::cout << "Creating the \"" << outputDir << "\" directory." << std::endl;
            std::filesystem::create_directory(outputDir);
        }

        extract_LoremIpsum_txt(outputDir, verbose);
        extract_bin_exe(outputDir, verbose);
    }

}

#endif
```

## Note

The compilation isn't efficient. Embedding a ~50MB file required ~10GB of memory during compilation!

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
