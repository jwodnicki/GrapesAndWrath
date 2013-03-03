using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace FileMaker
{
	class Program
	{
		static void Main(string[] args)
		{
			var wordSourceMap = new Dictionary<string, int>(){
				{"Zynga"  , 1 << 0},
				{"TWL 06" , 1 << 1},
				{"SOWPODS", 1 << 2}
			};

			var words = new Dictionary<string, int>();

			string word;
			foreach (KeyValuePair<string, int> kv in wordSourceMap)
			{
				using (TextReader reader = new StreamReader(@"..\..\Resources\" + kv.Key + ".txt"))
				{
					while ((word = reader.ReadLine()) != null)
					{
						if (words.ContainsKey(word))
						{
							words[word] |= kv.Value;
						}
						else
						{
							words[word] = kv.Value;
						}
					}
				}
			}
			using (FileStream fs = File.Create(@"..\..\Resources\All.gz"))
			using (GZipStream writer = new GZipStream(fs, CompressionMode.Compress))
			{
				foreach (KeyValuePair<string, int> kv in words.OrderBy(x => x.Key))
				{
					byte[] s = Encoding.ASCII.GetBytes(kv.Key + ',' + kv.Value + '\n');
					writer.Write(s, 0, s.Length);
				}
			}
		}
	}
}
