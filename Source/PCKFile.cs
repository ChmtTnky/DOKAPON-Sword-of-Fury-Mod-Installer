using System.Text;

// A PCK files has: (Size - Desc)
// Filename Section
// 0x14 - "Filename" with spaces as padding
// 0x04 - int32 section size (without accounting for padding at the end)
// 0x04... - int32 array of offsets to the file names (relative to the start of this array)
// 0x??... - array of null terminated file name strings
// 0x?? - 0x00 padding (8 byte alignment)
// Pack Section
// 0x14 - "Pack" with spaces as padding
// 0x04 - int32 section size (not including end padding or OGG data)
// 0x04 - int32 file count
// 0x08... - int32 offset to ogg data (relative to the origin) and int32 file size
// 0x04 - 0x00 padding (8 byte alignment)
// 0x??... - OGG data (each file is padded to 16 byte alignment before writing)
// 0x?? - 0x00 padding (8 byte alignment)

// In code, a PCK file is just a list of Sounds

namespace Sword_of_Fury_Mod_Installer
{
	public class PCKFile
	{
		private readonly string FILENAME_HEADER = "Filename";
		private readonly string PACK_HEADER = "Pack";
		private readonly int HEADER_SIZE = 0x14;

		public List<Sound> sounds { get; private set; }

		public PCKFile()
		{
			sounds = new List<Sound>();
		}

		public PCKFile(string in_file)
		{
			sounds = new List<Sound>();
			byte[] pck_data = File.ReadAllBytes(in_file);

			// Read Filename Section
			int filename_size = BitConverter.ToInt32(pck_data, HEADER_SIZE);
			int sound_count = BitConverter.ToInt32(pck_data, HEADER_SIZE + 4) / 4;

			// read sound names
			List<string> sound_names = new List<string>();
			for (int i = 0; i < sound_count; i++)
			{
				int curr_name_offset = BitConverter.ToInt32(pck_data, HEADER_SIZE + 4 + (i * 4)) + HEADER_SIZE + 4;
				string curr_file_name = string.Empty;
				while (pck_data[curr_name_offset + curr_file_name.Length] != 0x00)
					curr_file_name += (char)pck_data[curr_name_offset + curr_file_name.Length];
				sound_names.Add(curr_file_name);
			}

			// Read Pack Section
			int pack_offset = filename_size;
			if (pack_offset % 8 != 0)
				pack_offset += 8 - (pack_offset % 8);
			for (int i = 0; i < sound_count ;i++)
			{
				int curr_sound_data_offset = BitConverter.ToInt32(pck_data, pack_offset + (HEADER_SIZE + 8) + (i * 8));
				int curr_sound_data_size = BitConverter.ToInt32(pck_data, pack_offset + (HEADER_SIZE + 8) + (i * 8) + 4);

				sounds.Add(new Sound(sound_names[i], pck_data.Skip(curr_sound_data_offset).Take(curr_sound_data_size).ToArray()));
            }
		}

        public void Write(string output_path)
        {
            // begin with the Filename section
            List<byte> filename_section_data = new List<byte>();

            // get byte array of sound names, and their offsets in the array
            List<int> sound_name_offsets = new List<int>();
			List<byte> sound_name_bytes = new List<byte>();
			foreach(var sound in sounds)
			{
				sound_name_offsets.Add(sound_name_bytes.Count + (sounds.Count * 4));
                sound_name_bytes = sound_name_bytes.Concat(Encoding.ASCII.GetBytes(sound.name)).ToList();
				sound_name_bytes.Add(0x00);
			}

            // start writing Filename section
            // header
            filename_section_data = filename_section_data.Concat(FormatHeader(FILENAME_HEADER)).ToList();
			// size (to be filled later)
			filename_section_data = filename_section_data.Concat(new byte[4]).ToList();
			// name offsets
			foreach (var offset in sound_name_offsets)
				filename_section_data = filename_section_data.Concat(BitConverter.GetBytes(offset)).ToList();
			// name array
			filename_section_data = filename_section_data.Concat(sound_name_bytes).ToList();
			// size
			byte[] filename_section_size = BitConverter.GetBytes(filename_section_data.Count);
			for (int i = 0; i < filename_section_size.Length; i++)
				filename_section_data[i + HEADER_SIZE] = filename_section_size[i];
			// padding
			if (filename_section_data.Count % 8 != 0)
			{
				for (int i = 8 - (filename_section_data.Count % 8); i > 0; i--)
					filename_section_data.Add(0x00);
			}

			// then generate the Pack section
			List<byte> pack_section_data = new List<byte>();

			// get the length of the section data
			int pack_section_length = 0x1C + (sounds.Count * 8);
            // get the byte arrays for offsets and sound data
            List<byte> sound_data_bytes = new List<byte>();
			List<byte> sound_data_info_bytes = new List<byte>();
			foreach (var sound in sounds)
			{
				sound_data_info_bytes = sound_data_info_bytes.Concat(BitConverter.GetBytes(sound_data_bytes.Count + (pack_section_length + 4) + filename_section_data.Count)).ToList();
				sound_data_info_bytes = sound_data_info_bytes.Concat(BitConverter.GetBytes(sound.data.Length)).ToList();
				sound_data_bytes = sound_data_bytes.Concat(sound.data).ToList();
				if (sound.data.Length % 16 != 0)
				{
					for (int i = 16 - (sound.data.Length % 16); i > 0; i--)
						sound_data_bytes.Add(0x00);
				}
			}

			// start writing the Pack section data
			// header
			pack_section_data = pack_section_data.Concat(FormatHeader(PACK_HEADER)).ToList();
			// size and count
			pack_section_data = pack_section_data.Concat(BitConverter.GetBytes(pack_section_length)).ToList();
			pack_section_data = pack_section_data.Concat(BitConverter.GetBytes(sounds.Count)).ToList();
			// info bytes and padding
			pack_section_data = pack_section_data.Concat(sound_data_info_bytes).ToList();
			pack_section_data = pack_section_data.Concat(BitConverter.GetBytes(0x00000000)).ToList();
			// data bytes
			pack_section_data = pack_section_data.Concat(sound_data_bytes).ToList();

            // concatenate the sections
            List<byte> output_data = output_data = filename_section_data.Concat(pack_section_data).ToList();
            // add padding
            if (output_data.Count % 16 != 0)
            {
                for (int i = 16 - (output_data.Count % 16); i > 0; i--)
                    output_data.Add(0x00);
            }

            // output data to file
            File.WriteAllBytes(output_path, output_data.ToArray());
        }

		public void AddSound(string file_path)
		{
			sounds.Add(new Sound(file_path));
		}

		public void AddSound(Sound in_sound)
		{
			sounds.Add(in_sound);
		}

		// Pad a string to 0x14 with spaces
        private byte[] FormatHeader(string header_name)
		{
			if (header_name.Length > HEADER_SIZE)
				throw new Exception($"PCK header name is too long: {header_name}");

			return Encoding.ASCII.GetBytes(header_name + new string(' ', HEADER_SIZE - header_name.Length));
		}
	}

	// A Sound has a name and an array of data
	public class Sound
	{
		public string name { get; private set; }
		public byte[] data { get; private set; }

		public Sound(string file_path)
		{
			name = Path.GetFileName(file_path);
			data = File.ReadAllBytes(file_path);
		}

		public Sound(string file_name, byte[] data)
		{
			this.name = file_name;
			this.data = data;
		}

		public void Write(string output_dir = null)
		{
			File.WriteAllBytes(Path.Combine(output_dir, name), data);
		}
	}
}
