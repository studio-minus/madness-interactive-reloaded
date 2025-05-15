# Big Gustave

![NuGet](https://img.shields.io/nuget/dt/BigGustave?style=flat-square)

#### Open, read and create PNG images in fully managed C#.

## Usage - **Reading**

To open a PNG image from a file and get some pixel values:

```csharp
using (var stream = File.OpenRead(@"C:\my\file\path\file.png"))
    {
        Png image = Png.Open(stream);

        Pixel pixel = image.GetPixel(image.Width - 1, image.Height - 1);

        int pixelRedAverage = 0;

        pixelRedAverage += pixel.R;

        pixel = image.GetPixel(0, 0);

        pixelRedAverage += pixel.R;

        Console.WriteLine(pixelRedAverage / 2.0);

    }
```

#### The PNG object has multiple methods to inspect the header and get the pixel values. The header has properties for:
------

```csharp
png.Header.Width
png.Header.Height
png.Header.BitDepth
png.Header.ColorType
png.Header.CompressionMethod
png.Header.FilterMethod
png.Header.InterlaceMethod
```
------

**The PNG also has `Width`and `Height` as convenience properties from the header information:**

```csharp
png.Width == png.Header.Width
png.Height == png.Header.Height
```

**And a property that indicates whether the image uses transparency:**

```csharp
png.HasAlphaChannel
```

#### To get a pixel use:

```csharp
Pixel pixel = png.GetPixel(0, 7);
```

Where the first argument is x (column) and the second is y (row). The `Pixel` is used for all types of images (e.g. Grayscale, Color, with / without transparency)

-----

## Usage - **Creating**

Generating a PNG image:


```csharp
var builder = PngBuilder.Create(2, 2, false);

var red = new Pixel(255, 0, 0);

builder.SetPixel(red, 0, 0);
builder.SetPixel(255, 120, 16, 1, 1);

using (var memory = new MemoryStream())
    {
        builder.Save(memory);

        return memory.ToArray();
    }
```
-------

#### **You can load a PNG image into a builder which will copy all the pixels values into the builder for easier editing:**

```csharp
var png = Png.Open(@"C:\files\my.png");
var builder = PngBuilder.FromPng(png);
```
