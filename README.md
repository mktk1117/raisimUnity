# RaiSimUnity

![raisimunity gif](Images/raisimunity.gif)
 
RaiSimUnity is a visualizer for RaiSim based on [Unity](https://unity.com/). It gets the simulation data from raisim server application via TCP/IP.

The project was tested on Ubuntu 18.04 LST, Windows10 and MacOS.

## Dependencies

The following Unity plugins are already included in the project.                
- [SimpleFileBrowser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)

The following Unity packages are imported by Unity Package Manager automatically: see [Packages/manifest.json](Packages/manifest.json) 
- [UnityMeshImporter](https://github.com/eastskykang/UnityMeshImporter)

- vulkan. check https://linuxconfig.org/install-and-test-vulkan-on-linux

The follwoing package must be installed if you are using Linux
- minizip ```sudo apt install minizip```

The followings are optional dependencies
- [ffmpeg](https://www.ffmpeg.org/) for video recording
    - You can install ffmpeg by 
        ```sh
        $ sudo apt install ffmpeg
        ``` 

## How to 

### Using RaiSimUnity

Please follow instructions in this [link](https://raisim.com/sections/RaisimUnity.html)


### Development

The following sections introduces how to develop RaiSimUnity

-[Developing wiki](https://github.com/leggedrobotics/RaiSimUnity/wiki/developing).

## FAQ

- Is RaiSimUnity a stand-alone simulator? 
    - No, RaiSimUnity is a TCP client of [RaiSim](https://github.com/raisimtech/raisimLib).
- Is RaiSimUnity open-sourced?
    - Yes. However note that RaiSim is not an open-sourced project. 
- Can RaiSim and RaiSimUnity run on different machines? 
    - Yes. You can run RaiSim application on your "Server" machine and connect RaiSimUnity to the server by specifying IP address. 
- Does RaiSimUnity support Mac or Windows?
    - Yes
