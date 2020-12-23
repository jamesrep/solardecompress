// James - 2020.
// Just a simple c# code to decode the solarwinds stage 1 strings found in the fireye-compromise .
// Can scan decompiled c#-source code files and decode the obfuscated strings.
// Example:
// mono solardecompress2.exe --decodefile --regex --file /home/james/src/solarwinds/SolarWinds.Orion.Core.BusinessLayer/OrionImprovementBusinessLayer.cs > decoded_OrionImprovementBusinessLayer.cs


using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace solardecompress
{
    // Who has the copyright of this class found in OrionImprovementBusinessLayer.cs ?
    // slightly renamed only to avoid potentially crude yara/AV detection etc. otherwise the same as the original.
    public static class ZHelperFoundInBackdoor
    {
        public static byte[] Compress(byte[] input)
        {
            using (MemoryStream memoryStream1 = new MemoryStream(input))
            {
                using (MemoryStream memoryStream2 = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream((Stream)memoryStream2, CompressionMode.Compress)) memoryStream1.CopyTo((Stream)deflateStream);
                    return memoryStream2.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            using (MemoryStream memoryStream1 = new MemoryStream(input))
            {
                using (MemoryStream memoryStream2 = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream((Stream)memoryStream1, CompressionMode.Decompress))
                        deflateStream.CopyTo((Stream)memoryStream2);
                    return memoryStream2.ToArray();
                }
            }
        }

        public static string Zip(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            try
            {
                return Convert.ToBase64String(Compress(Encoding.UTF8.GetBytes(input)));
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string Unzip(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            try
            {
                return Encoding.UTF8.GetString(Decompress(Convert.FromBase64String(input)));
            }
            catch (Exception ex)
            {
                return input;
            }
        }
    }

    // This is my simple class to make the decoding process.
    class Program
    {
        static void printDecoded(bool bModeDecode, string strArg)
        {
            try
            {
                if (bModeDecode)
                {
                    Console.WriteLine(string.Format("{0,-55}   {1,-50}", ZHelperFoundInBackdoor.Unzip(strArg).Replace("\n", "\\n"), strArg));
                }
                else
                {
                    Console.WriteLine(string.Format("{0,-55}   {1,-50}", ZHelperFoundInBackdoor.Zip(strArg), strArg));
                }

            }
            catch
            {
                Console.WriteLine("[-] Error when decoding " + strArg);
            }
        }

        static void Main(string[] args)
        {
            bool bModeDecode = true;
            bool bRegex = false;
            bool bDecodeFileMode = false;

            if (args.Length < 1)
            {
                Console.WriteLine("--file <filename>    - use file to search for strings to decode");
                Console.WriteLine("--regex              - Scan inputs for regex");
                Console.WriteLine("--encode             - Set encode mode");
                Console.WriteLine("--decode             - Set decode mode (default)");
                Console.WriteLine("--decodefile         - Decode the whole file and change the source code output to stdout");

                Console.WriteLine();
                Console.WriteLine("Example: solardecompress --regex --file OrionImprovementBusinessLayer.cs");

                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string strArg = args[i];

                if (strArg.IndexOf("--file") == 0)
                {
                    i++;
                    strArg = args[i];

                    string[] strAll = File.ReadAllLines(strArg);

                    foreach (string str in strAll)
                    {
                        string strCarved = str;

                        if (bRegex)
                        {
                            MatchCollection mRex = Regex.Matches(str, "\\(\\s{0,}\"(?<base64>[A-z|0-9|\\/|=]{1,})\"\\s{0,}");

                            if (mRex.Count > 0)
                            {
                                string strDecodedLine = str;


                                for (int g = 0; g < mRex.Count; g++)
                                {

                                    string strVal = mRex[g].Groups["base64"].Captures[0].Value;


                                    if (bDecodeFileMode)
                                    {
                                        string strToReplace = "OrionImprovementBusinessLayer.ZipHelper.Unzip(\"" + strVal + "\")";
                                        string strDecoded = "\"" + ZHelperFoundInBackdoor.Unzip(strVal) + "\"";

                                        strDecodedLine = strDecodedLine.Replace(strToReplace, strDecoded);
                                    }
                                    else
                                    {
                                        printDecoded(bModeDecode, strVal);
                                    }
                                }

                                if (bDecodeFileMode)
                                {
                                    Console.WriteLine(strDecodedLine);
                                }
                            }
                            else if(bDecodeFileMode)
                            {
                                Console.WriteLine(str);
                            }
                        }
                        else
                        {
                            printDecoded(bModeDecode, str);
                        }
                    }
                }
                else if (strArg.IndexOf("--regex") == 0)
                {
                    bRegex = true;
                }
                else if (strArg.IndexOf("--decodefile") == 0)
                {
                    bDecodeFileMode = true;
                }
                else if (strArg.IndexOf("--noregex") == 0)
                {
                    bRegex = false;
                }
                else if (strArg.IndexOf("--encode") == 0)
                {
                    bModeDecode = false;
                }
                else if (strArg.IndexOf("--decode") == 0)
                {
                    bModeDecode = false;
                }
                else
                {
                    printDecoded(bModeDecode, strArg);
                }
            }
        }
    }
}