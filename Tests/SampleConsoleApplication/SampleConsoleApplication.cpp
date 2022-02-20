// This file uses the <filesystem> header, thus it needs to be compiled with a compiler that suports c++17.
#include "../../Output/embedded-extractor.h"
#include <iostream>

int main(int argc, char* argv[])
{
	if (argc > 1)
		embedded::extractAll(argv[1]);
	else
		embedded::extractAll();
	return 0;
}
