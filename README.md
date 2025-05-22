# DamageSegmentationXR (Developed and tested with Unity 2022.3.47f1 and Visual Studio 2022)

This repository contains the development code for an extended reality application for damage segmentation using deep learning. The application presented here corresponds to the work published in the paper ["Integrating extended reality and AI-based damage segmentation for near real-time, traceable bridge inspections"](https://doi.org/10.1016/j.autcon) by Pantoja-Rosero et al. (2025). You can access videos showcasing the use of the app at: [COCO model](https://youtu.be/qaj_2DKMpNA?si=l-_t4wXconsUqwHe), [cracks model](https://youtu.be/-hpc1krEwCc), [multiclass damage model](https://youtu.be/V-JF5vs8LLw). For detailed instructions on how to develop the application, including initial setup of Unity and Visual Studio, refer to the [manual](docs/manual/manual_app_development.pdf).

<p align="center">
  <img src=docs/images/fig_using_AI_HL.png width="20%">
</p>

<p align="center">
  <img src=docs/images/AI_XR_damages_1.png>
</p>

<p align="center">
  <img src=docs/images/AI_XR_damages_2.png>
</p>

**Note:** This work was inspired by the publicly available documentation of [Mr. Joost van Schaik](https://localjoost.github.io/). Some of his code has been adapted and integrated into the development of this application.

## How to build and deploy on HoloLens 2

### 1. Clone repository

Clone the repository to your local machine. This folder contains all the code and models necessary to deploy the app on the HoloLens 2.

### 2. Add project to Unity Hub

Open Unity Hub. Click on `Add` and select `Add project from disk`.

<p align="center">
  <img src=docs/images/instructions_1.png width="50%">
</p>

In the file explorer window, locate the downloaded repository folder `DamageSegmentationXR`, open it, and click on `Add Project`.

<p align="center">
  <img src=docs/images/instructions_2.png width="50%">
</p>

Click on the recently added project `DamageSegmentationXR` to open it in the Unity Editor. Note that the app was developed with the `2022.3.47f1` editor, which is recommended to avoid compatibility issues.

<p align="center">
  <img src=docs/images/instructions_3.png width="50%">
</p>

This will open windows displaying the Scene and Game view of the application.

<p align="center">
  <img src=docs/images/instructions_4.png width="50%">
</p>

### 3. Modify parameters and build the app

Optionally, before building the app, you may explore and modify various parameters. To do this, click on the `GameManager` game object in the `Hierarchy` window, which will display the `Inspector` window containing the configurable parameters.

<p align="center">
  <img src=docs/images/instructions_5.png width="50%">
</p>

Fix any issues by clicking on the `Edit` menu, then selecting `Project Settings`.

<p align="center">
  <img src=docs/images/instructions_6.png width="50%">
</p>

In the left panel, locate `Project Validation` under `XR Plug-in Management`. This will display a list of existing issues.

<p align="center">
  <img src=docs/images/instructions_7.png width="50%">
</p>

Click on `Fix All`, then close the window.

<p align="center">
  <img src=docs/images/instructions_8.png width="50%">
</p>

Click on the `File` menu and select `Build Settings...`.

<p align="center">
  <img src=docs/images/instructions_9.png width="50%">
</p>

Select the `Scenes/DamageSegmentationScene` and then click on `Build`.

<p align="center">
  <img src=docs/images/instructions_10.png width="50%">
</p>

This will open the file explorer within the project folder.

<p align="center">
  <img src=docs/images/instructions_11.png width="50%">
</p>

Click on `New Folder` and create a folder named `builds`.

<p align="center">
  <img src=docs/images/instructions_12.png width="50%">
</p>

Open the `builds` folder and click on `Select Folder`.

<p align="center">
  <img src=docs/images/instructions_13.png width="50%">
</p>

This will initiate the compilation of the application.

<p align="center">
  <img src=docs/images/instructions_14.png width="50%">
</p>

Once completed, the project folder will open automatically. Inside, open the `builds` folder containing the compiled application and double-click on the `DamageSegmentationXR.sln` file.

<p align="center">
  <img src=docs/images/instructions_15.png width="50%">
</p>

This will launch Visual Studio 2022, which will be used to deploy the app to the HoloLens 2.

<p align="center">
  <img src=docs/images/instructions_16.png width="50%">
</p>

Although deployment over Wi-Fi is possible, using a USB connection is more straightforward. Connect the HoloLens 2 to the PC via USB cable. Then, in the top toolbar, set the deployment options to: `Release` – `ARM64` – `Device`. Click on `Start Without Debugging` (or press Ctrl + F5).

<p align="center">
  <img src=docs/images/instructions_17.png width="50%">
</p>

If the process is successful, the application will be automatically deployed and launched on the HoloLens 2. When opened for the first time, grant the application permission to access the microphone, eye tracker, and camera. After that, the available menus will be displayed.

<p align="center">
  <img src=docs/images/instructions_18.jpg width="50%">
</p>

You can now select any model you wish to use, e.g., the COCO model.

<p align="center">
  <img src=docs/images/instructions_19.jpg width="50%">
</p>

Toggle the inference button and enjoy.

<p align="center">
  <img src=docs/images/instructions_20.jpg width="50%">
</p>

### 4. Citation

We kindly ask that you cite the following article if you use this project:

Paper:
```
@article{Pantoja-Rosero2025a,
title = {Integrating extended reality and AI-based damage segmentation for near real-time, traceable bridge inspections},
journal = {Automation in Construction},
volume = {},
pages = {},
year = {2025},
issn = {},
doi = {https://doi.org/10.1016/j.autcon},
url = {},
author = {B.G. Pantoja-Rosero and S. Salamone},
}
```

### 5. Acknowledgments

This work is part of the project *Advanced Vision-Based Assessment of Infrastructure Systems*, supported by the Exploratory Advanced Research (EAR) Program 2022 of the U.S. Federal Highway Administration (FHWA) under Cooperative Agreement Award 693JJ32350031.

The authors would like to thank the Concrete Bridge Engineering Institute (CBEI) for providing access to the bridge specimens used during the experimental validation of the proposed framework.
