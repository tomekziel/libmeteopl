libmeteopl
==========

Library for parsing weather forecast images from meteo.pl website.

Library is written in **C#** and is automatically translated to **Java** using .NET Compiler Platform ("Roslyn"). Looking for C#/Web developer to prepare C# to **Javascript** transformation class.

**Warning! This lib is currently a PoC and work-in-progress, image parser coverage is at minimal level.**

Sample input
============

![Sample input img](https://github.com/tomekziel/libmeteopl/blob/master/Docs/mgram.png "Sample input img")


Implemented data categories
=============

* Section 1 - temperature
  * Temperature (red line)
  * Temperature min/max (light red) - **incomplete/lack of heuristic**
  * Perceptible temperature (blue line) - **incomplete/lack of heuristic**
  * Perceptible temperature min/max (light blue) - **incomplete/lack of heuristic**
  * Dew point - **TBD**

* Section 2 - precipitation
  * all categories **TBD**

* Section 3 - atmospheric pressure
  * Pressure in hPa
  * Pressure in mm Hg

* Section 4 - wind speed
  * all categories **TBD**

* Section 5 - wind direction
  * all categories **TBD**

* Section 5 - clouds base and visibility
  * all categories **TBD**

* Section 6 - clouds coverage and fog
  * all categories **TBD**

 
How to use
==========

Be warned - work in progress, no nice environment for using this library yet.

C# code was edited using Visual Studio 14 CTP, project/workspace files are provided. If you are going to use earlier version, first manage to have Roslyn working.

Java code sample was edited using IntelliJ Idea 14, project/workspace files are provided.

Changelog
=========

Versioning not really started, waiting until data category coverage grow substantially.


Legal
=====

This is just a parser, it reads image like OCR reads words. The use of this Program does not entitle the User to claims against the Authors; Authors are not responsible for the consequences of use of the Program by users. 


Author
======

Library was created by Tomasz Zieliński (tzielinski@pgs-soft.com).
Part of R&D was done on company time while working in [PGS Software SA](http://www.pgs-soft.com)

License
=======

LibMeteoPL for Android

Copyright (c) 2015 PGS Software SA

https://github.com/tomekziel/libmeteopl

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
