LEProc Minified
===============

A minified locale emulator with command-line support only.

Many features, including automatic update checks and Locale Emulator Profile, are removed in order to keep the program as simple as possible.

Original source code is available at <https://github.com/xupefei/Locale-Emulator>.

## Usage ##
`LEProc.exe path [args]` where `path` is the relative or absolute path of the application to open and `args` will be passed to the application.

In addition to removed features, LEProc Minified is different from the original version in the following ways.

1. `path` can be either relative path or an executable file name in the environment variable `PATH`.
2. `.exe` part in the filename can be omitted. If the `path` does not end with `.exe`, `.exe` will be appended to the `path`, which means you cannot start non-executable file with LEProc Minified. To use an external program to open your file, specify it explicitly like `LEProc notepad path/to/your/file.txt`.  
In the original version, LEProc will start the associated program for you.
3. When the file is not found, a messagebox will be displayed.   
The error is suppressed in the original version.
4. The current directory will remain unchanged.  
In the original version, it is changed to the folder containing `path`.

## Build ##
This project can be built with Microsoft Visual Studio 2017.

## License ##

![LGPL V3](http://www.gnu.org/graphics/lgplv3-147x51.png)

[LGPL-3.0](https://opensource.org/licenses/LGPL-3.0).

The project is modified from [Locale-Emulator](https://github.com/xupefei/Locale-Emulator), which is licensed under LGPL-3.0.
