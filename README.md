# The DOKAPON! Sword of Fury Mod Installer
A program the streamlines modification of the Steam version of DOKAPON! Sword of Fury. 

## Basic Usage
To install mods, place the folders of mod files into the Mods folder of your download, and then run the program. It will prompt you to tell it where your game EXE is, and provide instructions on how to get there. It will then quickly apply the modifications to your game.

The program wil only search for mod files in the immediate folders you give it, so if it says it cannot find any mod files, it is likely because you places a mod inside of another folder. To fix this, move the files up the folder structure until the program properly finds them.

To uninstall your mods, verify the integrity of your game files in Steam.

## Usage for Modders
There are currently 4 types of mod: Assets, Sounds, Video, and Hex. The installer will also apply changes in that order.

To ensure your mod is applied correctly, you must place the changes of type in the correct folder. An example folder structure might look like this:
```
MyMod
-> Assets
  -> Map
    -> B_00.mdl
-> Sounds
  -> BGM03.wav
  -> BGM03.loop
-> Video
  -> Opening-en.mp4
-> Hex
  -> MoldeNerf.hex
```
### Asset Mods
The installer will treat the Assets folder as though it has the same folder structure as the "GameData\\app" folder in your game's installation. Every new file must have the same location and name as the file you wish to overwrite. The encoding and preparation of each file is left up to the modder, with the installation automating the process of moving the modded files into the game.

### Sounds
The installer will look at all files in the Sounds folder that lack the ".loop" extension, and treat them as sound files. If the mod file has the same name (exluding extension) as a file inside of one of the game's PCK files, the original sound file will be overwritten. Additionally, the installer will look for a ".loop" file with the same name as the mod file, and use it to determine what to write as the LoopStart and LoopEnd positions for the modded sound file. The format specification for the ".loop" file is:
```
number string for start of loop in samples
number string for end of loop in samples
```
Essentially, a ".loop" file contains two lines of text, where each line is just a number that tells the installer the position in samples of a loop endpoint. The format contains nothing else. If a line is empty, then the specific comment will be omitted, which may be necessary for some sound files.

After reading the loop data, the installer will use FFMPEG to convert your mod file into a WAV file, then into an OPUS file with the loop data baked in. It will then be written to the correct PCK file. As such, you do not have to do any work other providing a sound file and its loop data, with the installer doing the rest for you.

### Video Mods
The installer will get every file in the Video folder, assign each file with an OGV file to overwrite (if one exists), then convert each video into an OGV file using FFMPEG and VLC. Resizing and reformatting the video is done automatically by the installer. As such, all you must do is provide a video file with a name that matches an existing OVG file, and the installer will apply it with the correct settings.

WARNING: Sometimes the game crashes when playing custom videos. I don't know why this happens, but it has only occurred with longer videos. You can avoid this my skipping the video when the game attempts to play it.

### Hex Mods
The installer will search the Hex folder for all ".hex" files, and use the information contained in them to apply hex edits to the game's executable. The HEX format has the following specification:
```
 8 bytes: Starting offset
 8 bytes: Size of data
 X bytes: Data to write at the offset
 ... repeat ...
```
Every hex file contains an arbitrary amount of hex edits, and the installer will apply as many as it can, even if they conflict with other mods. The installer does not validate your edits, so there is no guarantee that your changes won't crash the game, nor that they will work on every platform. Please thoroughly test your changes before sharing them.

Note: HEX files are not text files, so they have to be written manually using a hex editor.

## Future Plans
- Automatic code edits
- Automatic file compression for certain formats
- Internal file edits/imports
