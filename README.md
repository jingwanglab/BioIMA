# BioIMA

**BioIMA** is a cross-platform desktop application for biological image measurement and analysis. It provides an interactive workflow for image annotation, scale calibration, SAM-assisted segmentation, automatic counting, batch measurement of whole image folders, morphological measurement, and color analysis.

BioIMA is designed for biological images such as seeds, petals, leaves, flowers, inflorescences, and other organismal or plant phenotyping images. It combines manual annotation tools with prompt-based segmentation and quantitative measurement functions in a local desktop environment.

<img width="9978" height="7215" alt="Fig 1 2_01" src="https://github.com/user-attachments/assets/e373b708-0c94-403c-b1f4-a1e3d70c0a10" />
<img width="3200" height="1950" alt="K1_batch" src="https://github.com/user-attachments/assets/066b1664-9ba5-4495-8a2c-1eb85525afab" />


---

## Features

**Annotation and scale**

* Cross-platform desktop application built with Avalonia UI and .NET
* Manual annotation tools for polygons, rectangles, lines, angles, and a scale ruler
* Scale ruler with draggable end points: place two points on a known length, adjust them, then enter the length and unit (mm, cm, μm, nm, inch). A scale bar that follows the zoom level is shown at the bottom-left of the image
* Image folder browsing with previous / next image buttons
* Supports PNG, JPG, BMP, TIFF (including 16-bit and CMYK) and HEIC/HEIF images

**Segmentation**

* SAM-assisted segmentation with point and box prompts, with two selectable models: **SAM** (more accurate) and **MobileSAM** (much lighter and faster, recommended for large folders and slower computers)
* Accepted SAM masks become editable polygon labels and are measured automatically (can be switched off in Settings)
* **Edit Boundary** to drag, add or delete vertices; the Results row is updated when editing finishes
* **Replace selected label**: select a label, re-prompt the object with points or a box, and replace its outline and its Results row

**Counting and batch work**

* **Find similar**: segment one object, then find, outline and measure all similar objects in the image (adjustable size and colour tolerance, Ctrl/⌘+click to exclude an object)
* **Count** (Tools > Count Objects, or the 123 button): fully automatic counting with numbered badges on each object; objects of another kind can be counted by clicking them
* **Count folder**: count the objects in every image of the opened folder and export the counts as CSV
* **Batch Measure** (Tools > Batch Measure): use one segmented object as an example, then locate, outline with SAM and measure matching objects in every image of the opened folder
  * *Many per image* mode (seeds, berries, leaves...) or *One per image* mode (one main subject per image)
  * Optional expected count per image; images with a different count, or where SAM and the colour mask disagree, are flagged
  * Click any processed image in the list to view its saved result instantly
  * Results of all images are merged into one Results table and one CSV
* **Ignore colour** option: match objects by shape and size only, for objects that differ in colour but look alike

**Measurement**

* Morphological measurements: area, perimeter, width, height, aspect ratio, circularity, equivalent diameter, line length, and angle
* Color measurements: mean RGB, HEX, CIE Lab values, and color distribution
* K-means based color distribution analysis with selectable cluster number K
* Basic white/gray reference color correction for reducing lighting or scanner color bias
* Results Id is the folder name plus the counting order within each image (for example `batch2_1`, `batch2_2`); a custom prefix can be set in the Batch Measure panel

**Files**

* Annotation project saving and loading through JSON files
* Measurement result export to CSV

---

## Quick start

1. **Load > Open Folder** (or Open Image) and pick an image.
2. Set the scale with the **Ruler** tool (left toolbar or Tools > Ruler): click two points on a known length, drag the ends to adjust, enter the length and unit, and click **Set scale**.
3. Open **Segmentation** (brain icon or Tools > SAM Segmentation), choose **SAM** or **MobileSAM**, click on an object, and click **Accept Mask**. The object is added to the Results table.
4. For many similar objects, click **Find similar...**, or use **Count Objects** for a quick count.
5. For a whole folder, segment one example object, open **Tools > Batch Measure**, click **Use current object**, choose a mode, and click **Run on folder**.
6. **Save > Export Results CSV** writes the Results table.

Hover over a button or a panel title to see a short description of what it does.

---

## Limitations

* Find similar, Count and Batch Measure work best on images with a fairly uniform background. Objects that touch each other may be merged into one.
* The scale is global: all images in a session share the same scale, so set it again when the magnification changes.
* Batch review shows the saved result of each image; changes to a label can be made with Edit Boundary or Replace selected label.

---

## Installation

### Download release

Releases are provided on the GitHub Releases page.

1. Go to the **Releases** page.
2. Download the installer or application package for your operating system.
3. Launch BioIMA.

Supported platforms:

* macOS
* Windows

> Note: The exact release package names may vary between versions.

---

## macOS notes

On macOS, BioIMA includes adaptation for packaged application execution. SAM and MobileSAM model files are copied to a writable directory on first launch to avoid permission issues caused by read-only application bundle paths.

If macOS blocks the app because it was downloaded from the internet, open it using the standard macOS security workflow, such as right clicking the app and selecting **Open**.

---

## Build from source

### Requirements

* .NET 8 SDK
* Git
* macOS or Windows
* SAM ONNX model files (included in releases):
  * `SAMmodel/encoder-quant.onnx` and `SAMmodel/decoder-quant.onnx` for SAM
  * `SAMmodel/mobile/mobile_sam_image_encoder.onnx` and `SAMmodel/mobile/sam_mask_decoder_single.onnx` for MobileSAM

### Run

```bash
git clone <repository-url>
cd <repository-folder>/BioIMA.Avalonia
dotnet restore
dotnet run
```

### Package releases (maintainers)

`build-release.sh` publishes self-contained builds for Windows (x64) and macOS (Apple Silicon) and packs them with [Velopack](https://velopack.io) (`vpk` must be installed):

```bash
./build-release.sh 1.1.0   # version number is optional
```

The script first checks that all four model files exist, rebuilds the `publish-win` and `publish-mac` folders from scratch, verifies that the models were copied into each of them, and writes the installers to `Releases-win` and `Releases-mac`.

### Build and run

```bash
git clone <repository-url>
cd <repository-folder>
dotnet restore
dotnet build
dotnet run --project BioIMA.Avalonia
```

BioIMA.Avalonia (the application) references the BioIMA.Core project, which sits next to it in the same repository.

---

## Contact

For questions, issues, or feature requests, please use the GitHub Issues page.

## Acknowledgements

BioIMA uses open source tools and libraries including Avalonia UI, .NET, ONNX Runtime, Magick.NET, and SAM and MobileSAM model components.
