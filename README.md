# BioIMA

**BioIMA** is a cross-platform desktop application for biological image measurement and analysis. It provides an interactive workflow for image annotation, scale calibration, SAM-assisted segmentation, morphological measurement, and color analysis.

BioIMA is designed for biological images such as seeds, petals, leaves, flowers, inflorescences, and other organismal or plant phenotyping images. It combines manual annotation tools with prompt-based segmentation and quantitative measurement functions in a local desktop environment.

<img width="9978" height="7215" alt="Fig 1 2_01" src="https://github.com/user-attachments/assets/e373b708-0c94-403c-b1f4-a1e3d70c0a10" />

---

## 

* Cross-platform desktop application built with Avalonia UI and .NET
* Manual annotation tools for polygons, rectangles, lines, angles, and scale rulers
* SAM-assisted segmentation with point and box prompts
* Accepted SAM masks can be converted into editable labels
* Morphological measurements including area, perimeter, width, height, aspect ratio, circularity, equivalent diameter, line length, and angle
* Color measurements including mean RGB, HEX, CIE Lab values, and color distribution
* K-means based color distribution analysis with selectable cluster number K
* Basic white/gray reference color correction for reducing lighting or scanner color bias
* Annotation project saving and loading through JSON files
* Measurement result export to CSV

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

On macOS, BioIMA includes adaptation for packaged application execution. SAM model files are copied to a writable directory on first launch to avoid permission issues caused by read-only application bundle paths.

If macOS blocks the app because it was downloaded from the internet, open it using the standard macOS security workflow, such as right clicking the app and selecting **Open**.

---

## Build from source

### Requirements

* .NET 8 SDK
* Git
* macOS or Windows

---
## Contact

For questions, issues, or feature requests, please use the GitHub Issues page.


## Acknowledgements

BioIMA uses open source tools and libraries including Avalonia UI, .NET, ONNX Runtime, and SAM-related model components.
