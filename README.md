# CVHCI-HoloLens-app

This is part of a students project hosted by the CVHCI Institute of the Karlsruhe Institute of Technology. Goal of the project is inspecting a generated 3D model of an object that the user can select.
The instance segmentation and 3D model generation is performed in a separate server application.
This UWP app enables the user to perform a select gesture on an object, display the 3D generated model and interact with it.

# Prerequisites
- Windows 10 Build 10.0.10240
- Unity 2019.4
- Visual Studio 2019.8

# Install

1. Download or clone the repository
2. Import the project into Unity
3. Build for UWP using Unity
4. Open the generated Visual Studio Solution with Visual Studio 2019.8
5. Build package and install on HoloLens2

# Known Issues

- The Scene is not loading correctly:
    - Reload the scene using File -> Open Scene
- XR is not enabled
    - Edit -> Project Settings -> Player Settings -> Enable XR