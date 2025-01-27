using System.Diagnostics;
using System.Xml.Linq;

namespace Sword_of_Fury_Mod_Installer
{
    public static class Installer
	{
		public static readonly string MODS_DIR_NAME = "Mods";
		public static readonly string MODS_ASSETS_DIR_NAME = "Assets";
		public static readonly string MODS_CODE_DIR_NAME = "Codes";
		public static readonly string MODS_SOUND_DIR_NAME = "Sounds";
		public static readonly string MODS_HEX_DIR_NAME = "Hex";
        public static readonly string MODS_VIDEO_DIR_NAME = "Video";
        public static readonly string[] MODS_TYPE_DIR_ARRAY = { MODS_ASSETS_DIR_NAME, MODS_CODE_DIR_NAME, MODS_HEX_DIR_NAME, MODS_SOUND_DIR_NAME, MODS_VIDEO_DIR_NAME };

		public static readonly string GAME_ASSETS_DIR_NAME = "GameData\\app";
		public static readonly string GAME_EXE_FILE_NAME = "DOKAPON! Sword of Fury.exe";

		public static readonly string SAVED_CONTENT_DIR = "Backup";
        public static readonly string SAVED_INSTALL_TXT = Path.Combine(SAVED_CONTENT_DIR, "game_exe.txt");
        public static readonly string SAVED_INSTALL_EXE = Path.Combine(SAVED_CONTENT_DIR, GAME_EXE_FILE_NAME);

		public static readonly string HELPER_EXE_DIR_NAME = "RequiredEXEs";

        public static List<bool>? InstallMods()
		{
			List<bool>? output = new List<bool>();
			// check for mods folder
			if (!Directory.Exists(MODS_DIR_NAME))
			{
				Log.OutputError("Could not finds Mods folder\nExiting program...");
				return output;
			}

			// get and verify the path of the game exe
			string? true_exe_path = SetUpEXE();
			if (true_exe_path == null)
				return null;
			string? true_game_dir = Path.GetDirectoryName(true_exe_path);
            // save the game path for future use
            File.WriteAllText(SAVED_INSTALL_TXT, true_exe_path);
            Log.WriteLine($"Using \"{true_game_dir}\" for the installation...\n");

            // get folders of each mod type
            Dictionary<string, List<string>>? mod_file_dirs = GetModFiles();
            if (mod_file_dirs == null || mod_file_dirs.Sum(x => x.Value.Count) == 0)
            {
                Log.OutputError("No mod files found\nExiting Program...");
                return null;
            }

            // make copy of backup exe to modify
            string temp_exe_path = Path.Combine(Directory.GetCurrentDirectory(), GAME_EXE_FILE_NAME);
            File.Copy(SAVED_INSTALL_EXE, temp_exe_path, true);

			// run the individual install functions

			// assets modifies the raw game files
			output.Add(InstallAssetMods(true_game_dir, mod_file_dirs[MODS_ASSETS_DIR_NAME]));
            Log.WriteLine("");

            // sounds extracts then modifies the raw game files
            output.Add(InstallSoundMods(true_game_dir, mod_file_dirs[MODS_SOUND_DIR_NAME]));
            Log.WriteLine("");

            output.Add(InstallVideoModsAsync(true_game_dir, mod_file_dirs[MODS_VIDEO_DIR_NAME]).Result);
            Log.WriteLine("");

            // hex edits modifies the temp exe first
            output.Add(InstallHexEdits(Path.Combine(Directory.GetCurrentDirectory(), GAME_EXE_FILE_NAME), mod_file_dirs[MODS_HEX_DIR_NAME]));
            Log.WriteLine("");

            // codes modifies the temp exe second
            //output.Add(InstallCodeMods(Directory.GetCurrentDirectory(), mod_file_dirs[MODS_CODE_DIR_NAME]));
            //Log.WriteLine("");

            // copy the modded exe into the game files
            File.Copy(temp_exe_path, true_exe_path, true);

			// delete the temporary exe
			File.Delete(temp_exe_path);

			// Wrap up
			Log.WriteLine("Done.\n");
			return output;
		}

		// the logic that obtains the correct location of the exe to modify
		private static string? SetUpEXE()
		{
			// guarantee backup folder exists
			if (!Directory.Exists(SAVED_CONTENT_DIR))
				Directory.CreateDirectory(SAVED_CONTENT_DIR);

			string? true_exe_path = null;
			// check the text file
			if (File.Exists(SAVED_INSTALL_TXT))
                true_exe_path = File.ReadAllText(SAVED_INSTALL_TXT).Replace("\"", "").Trim();

			// If the saved path isn't valid, ask for a path
			if (!File.Exists(true_exe_path))
			{
				true_exe_path = RequestEXEPathFromUser();
				Log.WriteLine("");
				// If the manual path is not valid, output an error and exit
				if (!File.Exists(true_exe_path))
				{
					Log.OutputError($"Invalid executable path:\n{true_exe_path}\nExiting program...\n");
					return null;
				}
				// assume the user is new and backup their exe
				Log.WriteLine($"Backup EXE being saved at \"{SAVED_INSTALL_EXE}\"...");
				File.Copy(true_exe_path, SAVED_INSTALL_EXE, true);
			}

            // backup exe, but warn the user in case this is unintended
            if (!File.Exists(SAVED_INSTALL_EXE))
			{
                Log.OutputError("Backup EXE not found!\nVerify the integrity of your game files in Steam before pressing Enter...");
                Console.ReadKey();
                Log.WriteLine($"Backup EXE being saved at \"{SAVED_INSTALL_EXE}\"");
                File.Copy(true_exe_path, SAVED_INSTALL_EXE, true);
            }

            // output
            return true_exe_path;
		}

		// get the relevant Assets, Sounds, Hex, and Codes folder for each mod
		private static Dictionary<string, List<string>>? GetModFiles()
		{
			Log.WriteLine("Getting all mod files...");
			Dictionary<string, List<string>> output = new Dictionary<string, List<string>>();

			// initialize output dict
			foreach (string mod_type in MODS_TYPE_DIR_ARRAY)
				output.Add(mod_type, new List<string>());

			// to start, get an array of all mod folders in the Mods folder
			if (!Directory.Exists(MODS_DIR_NAME))
			{
				Log.OutputError($"{MODS_DIR_NAME} directory does not exist\nExiting Program");
				return null;
			}
			string[] mod_folders = Directory.GetDirectories(MODS_DIR_NAME);

			// for each mod folder, find the relevant folder names and add them to each list
			foreach (string mod_folder in mod_folders)
			{
				bool has_mods = false;
                // if subfolder of that type exists, add it to list of that type in dictionary
                for (int i = 0; i < MODS_TYPE_DIR_ARRAY.Length; i++)
				{
					if (Directory.Exists(Path.Combine(mod_folder, MODS_TYPE_DIR_ARRAY[i])))
					{
						output[MODS_TYPE_DIR_ARRAY[i]].Add(Path.Combine(mod_folder, MODS_TYPE_DIR_ARRAY[i]));
						has_mods = true;
					}
                }
				if (has_mods)
					Log.WriteLine($"Attemping to install \"{Path.GetFileName(mod_folder)}\"...");
				else
					Log.WriteLine($"\"{Path.GetFileName(mod_folder)}\" has no mods...");
            }
			Log.WriteLine("");

			// output dictionary
			return output;
		}

		// output instructions for the user on how to find the exe
		// the exe is gotten over the game directory because it is easier for end users to find and get the path
		private static string? RequestEXEPathFromUser()
		{
			Log.WriteLine(
				$"Please input the path of your installation's vanilla \"{GAME_EXE_FILE_NAME}\" file.\n" +
				"To get the file path, in Steam, Right Click on the game in your Library, then click Manage, then Browse Local Files.\n" +
				$"Then, to obtain the path of the \"{GAME_EXE_FILE_NAME}\" file, click on it, then Shift + Right Click it and select Copy as Path.\n" +
				"Then, paste it into this console and press Enter.\n" +
				"Executable Path: ");
			return Log.ReadLine()?.Replace("\"", "").Trim();
		}

		// installs all file replacement mods for non-sound files and non-archive files
		private static bool InstallAssetMods(string? true_game_dir, List<string> asset_folders)
		{
            Log.WriteLine($"Installing asset mods...");

            // Verify all prereqs
            // Check if game assets folder exists
            string? game_asset_dir = Path.Combine(true_game_dir, GAME_ASSETS_DIR_NAME);
			if (!Directory.Exists(game_asset_dir))
			{
				Log.OutputError($"Asset folder could not be found at:\n\"{game_asset_dir}\"\nSkipping asset mods...\n");
				return false;
			}

			// check if there are any assets to install
			if (asset_folders.Count == 0 || asset_folders == null)
			{
				Log.WriteLine("No mod assets were found.\nSkipping asset mods...");
				return false;
			}

			// copy each Assets folder into the game files
			int total_mods = 0;
			HashSet<string> already_modded_paths = new();
			foreach (string folder in asset_folders)
			{
				List<string> assets_to_copy = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToList();
				foreach (string asset in assets_to_copy)
				{
					string path_in_game_files = Path.Combine(game_asset_dir, Path.GetRelativePath(folder, asset));

					if (File.Exists(path_in_game_files))
					{
						if (!already_modded_paths.Add(path_in_game_files))
						{
							Log.OutputError($"Duplicate asset file found at \"{asset}\". Skipping...");
							continue;
						}
						Log.WriteToLog($"Overwriting \"{path_in_game_files}\" with file from \"{asset}\"...\n");
						File.Copy(asset, path_in_game_files, true);
						total_mods++;
					}
					else
						Log.OutputError($"\"{Path.GetRelativePath(folder, asset)}\" could is not a real game file");
				}
			}

			// output results
			if (total_mods > 0)
			{
				Log.WriteLine($"Replaced {total_mods} asset {(total_mods == 0 ? "file" : "files")}...");
				return true;
			}
			Log.OutputError($"No asset files were changed.");
			return false;
		}

		/*
		// uses dkcedit to install all code mods
		private static bool InstallCodeMods(string? temp_game_dir, List<string> codes_folders)
		{
            Log.WriteLine($"Installing code mods...");
            // get folders with code mods in them
            List<string> code_mod_dirs = new List<string>();
			foreach (string folder in codes_folders)
				code_mod_dirs = code_mod_dirs.Concat(Directory.GetDirectories(folder)).ToList();

			// check for mods
			if (code_mod_dirs.Count == 0)
			{
                Log.WriteLine($"Codes folder has no subdirectories\nSkipping code mods...\n");
                return false;
            }

            // set up args
            int total_mods = 0;
            string args = $"\"{temp_game_dir}\"";
			foreach (string folder in code_mod_dirs)
			{
                // check for mod files
                if (!File.Exists(Path.Combine(folder, "mod.bin")))
                {
                    Log.WriteLine($"{folder} does not have a mod.bin file\nSkipping mod...");
                    continue;
                }
                if (!File.Exists(Path.Combine(folder, "functions.txt")))
				{
					Log.WriteLine($"{folder} does not have a functions.txt file\nSkipping mod...");
					continue;
				}
                if (!File.Exists(Path.Combine(folder, "variables.txt")))
                {
                    Log.WriteLine($"{folder} does not have a variables.txt file\nSkipping mod...");
                    continue;
                }
				Log.WriteToLog($"Applying the {Path.GetFileName(folder)} mod...\n");
                args += $" \"{Path.GetFullPath(folder)}\"";
				total_mods++;
			}
			// run command
			Log.WriteLine($"Injecting {total_mods} code {(total_mods == 1 ? "mod" : "mods")}...");
			Log.WriteToLog($"Running DKCedit with: {args}\n");
			Process? apply_codes = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(HELPER_EXE_DIR_NAME, "dkcedit", "DKCedit.exe"),
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            string log_output = apply_codes.StandardOutput.ReadToEnd() + "\n" + apply_codes.StandardError.ReadToEnd();
            Log.WriteToLog(log_output);
            apply_codes?.WaitForExit();
            apply_codes?.Close();
			Log.WriteLine("");
            return true;
		}
		*/

		// uses the hex editor to apply all hex edits
		private static bool InstallHexEdits(string? temp_exe_path, List<string> hex_folders)
		{
			Log.WriteLine("Installing hex edits...");

			// no hex mods
			if (hex_folders.Count == 0)
			{
				Log.WriteLine("No Hex folders found. \nSkipping hex edits...");
				return false;
			}

			int total_mods = HexEditor.ApplyMods(hex_folders, temp_exe_path);
			Log.WriteLine($"Writing {total_mods} hex {(total_mods == 1 ? "edit" : "edits")}...\n");
			return (total_mods > 0);
		}

        // Replaces sound files with
        private static bool InstallSoundMods(string? game_dir, List<string> sound_folders)
        {
			Log.WriteLine("Installing sound mods...");

			// check for sound folders
			if (sound_folders.Count == 0)
			{
				Log.WriteLine("No Sound folders found. \nSkipping sound mods...");
				return false;
			}

			// get mod files
			// dict<name, path>
			Dictionary<string, string> mods = new Dictionary<string, string>();
			foreach (var sound_folder in sound_folders)
			{
				List<string> sound_files = Directory.GetFiles(sound_folder, "*", System.IO.SearchOption.AllDirectories).ToList();
				foreach(var sound_file in sound_files)
				{
					if (Path.GetExtension(sound_file) == ".loop")
						continue;
					if (!mods.TryAdd(Path.GetFileName(sound_file), sound_file))
						Log.OutputError($"Duplicate sound file found at \"{sound_file}\". Skipping...");
				}
			}

			// no mod files
			if (mods.Count == 0)
			{
				Log.OutputError("No sound mod files found. \nSkipping sound mods...");
				return false;
			}
			Log.WriteLine($"Found {mods.Count} sound {(mods.Count == 1 ? "file" : "files")}...");

            // get pcks in memory
            List<PCKFile> pcks = new List<PCKFile>();
            List<string> pck_paths = Directory.GetFiles(Path.Combine(game_dir, GAME_ASSETS_DIR_NAME), "*.pck", System.IO.SearchOption.TopDirectoryOnly).ToList();
            foreach (var pck_path in pck_paths)
            {
                pcks.Add(new PCKFile(pck_path));
            }

            // for each mod file, check each pck if it is there and replace it if it is
            int total_mods = 0;
            foreach (var mod_file in mods)
			{
				foreach (var pck in pcks)
				{
					int sound_to_replace = pck.sounds.FindIndex(x => Path.GetFileNameWithoutExtension(x.name) == Path.GetFileNameWithoutExtension(mod_file.Key));
					if (sound_to_replace != -1)
					{
						Log.WriteToLog($"Replacing \"{pck.sounds[sound_to_replace]}\" with \"{mod_file.Value}\"...\n");

						// get loop data
						string loop_file_path = Path.Combine(Path.GetDirectoryName(mod_file.Value), Path.GetFileNameWithoutExtension(mod_file.Key) + ".loop");
                        int start_pos = 0, end_pos = 0;
						if (File.Exists(loop_file_path))
						{
							string[] loop_lines = File.ReadAllLines(loop_file_path);
							if (loop_lines.Length >= 2)
							{
								// check for empty lines
								// output -1 if it is empty
								if (loop_lines[0].Length > 0)
									start_pos = Int32.Parse(loop_lines[0]);
								else
									start_pos = -1;

								if (loop_lines[1].Length > 0)
									end_pos = Int32.Parse(loop_lines[1]);
								else
									end_pos = -1;
							}
							else
								Log.OutputError($"Loop file for \"{mod_file.Value}\" has less than 2 lines. Skipping...");
						}
						else
							Log.WriteToLog($"{mod_file.Key} has no loop data. Using default value of 0...\n");

						// convert audio to wav temporarily
						string temp_wav_path = "temp_wav.wav";
						if (Path.GetExtension(mod_file.Key) != ".wav")
						{
							Log.WriteToLog($"Converting \"{mod_file.Key}\" to a WAV file...\n");
							FFMpegCore.FFMpegArguments
								.FromFileInput(mod_file.Value)
								.OutputToFile(temp_wav_path, true, options => options
									.ForceFormat("wav")
								.WithFastStart())
								.ProcessSynchronously();
						}
						else
						{
							Log.WriteToLog($"\"{mod_file.Key}\" is already a wav file. Skipping conversion...");
							File.Copy(mod_file.Value, temp_wav_path, true);
						}

						// convert audio to opus with loop
						string comment_string = (start_pos != -1 ? $"--comment \"LoopStart={start_pos}\" " : string.Empty) + (end_pos != -1 ? $"--comment \"LoopEnd={end_pos}\"" : string.Empty);
                        string new_opus_path = Path.GetFileNameWithoutExtension(mod_file.Key) + Path.GetExtension(pck.sounds[sound_to_replace].name);
                        Process? encode = Process.Start(new ProcessStartInfo()
                        {
                            FileName = Path.Combine(HELPER_EXE_DIR_NAME, "opusenc", "opusenc.exe"),
                            Arguments = $"{temp_wav_path} {new_opus_path} {comment_string}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        });
                        string log_output = encode.StandardOutput.ReadToEnd() + "\n" + encode.StandardError.ReadToEnd();
                        Log.WriteToLog(log_output);
                        encode?.WaitForExit();
                        encode?.Close();

                        // write to pck
                        pck.sounds[sound_to_replace] = new Sound(new_opus_path);
                        File.Delete(new_opus_path);
						File.Delete(temp_wav_path);
                        total_mods++;
					}
				}
			}

            // inform the user of the results
            if (total_mods != 0)
			{
				Log.WriteLine($"Replaced {total_mods} sound {(total_mods != 1 ? "files" : "file")}...");
                // write out the new pcks
                Log.WriteLine("Writing new PCKs...");
                for (int i = 0; i < pcks.Count; i++)
                    pcks[i].Write(pck_paths[i]);
                return true;
			}
			Log.OutputError("No sound mods could be applied.");
			return false;
        }

		// Encode and overwrite video files
		private static async Task<bool> InstallVideoModsAsync(string game_dir, List<string> video_folders)
		{
            Log.WriteLine("Installing video mods...");

			// check for video folders
			if (video_folders.Count == 0)
			{
				Log.WriteLine("No Video folders found. \nSkipping video mods...");
				return false;
			}

            // get mod files
            // dict<name, path>
            Dictionary<string, string> mods = new Dictionary<string, string>();
            foreach (var video_folder in video_folders)
            {
                List<string> video_files = Directory.GetFiles(video_folder, "*", System.IO.SearchOption.AllDirectories).ToList();
                foreach (var video_file in video_files)
                {
                    if (!mods.TryAdd(Path.GetFileNameWithoutExtension(video_file), video_file))
                        Log.OutputError($"Duplicate video file found at \"{video_file}\". Skipping...");
                }
            }

            // no mod files
            if (mods.Count == 0)
            {
                Log.OutputError("No video mod files found. \nSkipping sound mods...");
                return false;
            }

			// assign each video file with a file to overwite
			List<(string ogv, string mp4)> video_out_in_list = new();
			List<string> ogv_paths = Directory.GetFiles(Path.Combine(game_dir, GAME_ASSETS_DIR_NAME), "*.ogv", System.IO.SearchOption.TopDirectoryOnly).ToList();
			foreach (var mod in mods)
			{
				string? ogv_to_replace = ogv_paths.Find(x => Path.GetFileNameWithoutExtension(x) == Path.GetFileNameWithoutExtension(mod.Key));
                if (ogv_to_replace != null)
				{
					video_out_in_list.Add(new (ogv_to_replace, mod.Value));
				}
			}

			// no replacable files
			if (video_out_in_list.Count == 0)
			{
				Log.OutputError("No MP4s have an associated OGV to replace. \nSkipping video mods...");
				return false;
			}

			// convert each video file
			Log.WriteLine($"Installing {video_out_in_list.Count} video {(video_out_in_list.Count == 1 ? "mod" : "mods")}...\n" +
				$"WARNING: This may take a while.");
			foreach (var video_pair in video_out_in_list)
			{
				Log.WriteLine($"Converting \"{video_pair.mp4}\" to new MP4...");
				// convert video with correct specs
				string? converted_mp4_path = "temp_video_file.mp4";
				string converted_ogv_path = "temp_video_file.ogv";
				FFMpegCore.FFMpegArguments
					.FromFileInput(video_pair.mp4)
					.OutputToFile(converted_mp4_path, true, options => options
						.ForceFormat("mp4")
						.WithCustomArgument("-vf \"scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar=1\" -ar 48000 -r 29.97 -pix_fmt yuv420p -y")
					.WithFastStart())
					.ProcessSynchronously();

				// verify it exists
				if (!File.Exists(converted_mp4_path))
				{
					Log.OutputError($"Conversion of \"{video_pair.mp4}\" failed. Skipping to next video...");
					continue;
				}

                Log.WriteLine($"Converting new MP4 to new OGV...");
                // convert to ogv
                Process? encode = Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine(HELPER_EXE_DIR_NAME, "vlc", "vlc.exe"),
                    Arguments = $"--no-repeat --no-loop -I dummy {converted_mp4_path} " +
								$"--sout-theora-quality=8 --sout-vorbis-quality=4 " + 
								$"--sout=#transcode{{vcodec=\"theo\",acodec=\"vorb\",\"channels=2\"}}:" +
								$"standard{{access=\"file\",mux=\"ogg\",dst=\"{converted_ogv_path}\"}} " +
								$"vlc://quit",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                string log_output = encode.StandardOutput.ReadToEnd() + "\n" + encode.StandardError.ReadToEnd();
                Log.WriteToLog(log_output);
                encode?.WaitForExit();
                encode?.Close();

				// move and clean up
				File.Move(converted_ogv_path, video_pair.ogv, true);
                File.Delete(converted_mp4_path);
            }

            return true;
        }
    }
}
