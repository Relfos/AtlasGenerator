# Atlas Generator
Takes a folder of images (eg: PNG or JPG) and splits an atlas with every picture combined, optionally plus a CSS file.

## Game development
Packing multiple sprites into a single image file can be helpful for optimizating performance in some game engines.

## Web development
Useful for webpage optimization, allowing you to pack all images in the website inside a single file, allowing a webpage to load faster.

Example:
```
atlasgen.exe -input.path=D:\some\path\to\images\here -input.filter=*.jpg -prefix=team -atlas.extension=jpg -resize=240
```

## Options list

| Option  | Required  | Description  | Default   |
|---|---|---|---|
| -prefix | yes  | Specifies what prefix to use for output  |
| -input.path  | yes  | Specifies the path where the images to be used as input are located   | N/A |
| -input.filter  | no  | Specifies a filter to apply when selecting the input files (eg: \*.png )| \*.* | 
| -atlas.extension  | no  | Specifies the output atlas extension  |  jpg |
| -atlas.path  | no  | Specifies the output output path  | Current directory  |
| -resize  | no  | Resizes all images to this size  | 0  |
| -css.path  | no  | Enables generation of CSS as output, and specifies the output path   | N/A  |
| -json.path  | no  | Enables generation of JSON as output, and specifies the output path   | N/A  |
| -xml.path  | no  | Enables generation of XML as output, and specifies the output path   | N/A  |
| -csv.path  | no  | Enables generation of CSV as output, and specifies the output path   | N/A  |
| -margin.X  | no | Adds an margin (in pixels) to the left and right of each image  | 0 |
| -margin.Y  | no | Adds an margin (in pixels) to the top and bottom of each image  | 0 |
| -normalize  | no | Pads each image around with empty pixels to make every image occupy same space in atlas  | false |
