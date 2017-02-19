using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonTest_Speed
{
    class JsonTestSpeed
    {
        public static void Main(string[] args)
        {
            var settings = new Dictionary<object, object>()
            {
                ["working profile"] = "read speed test",
                ["profiles"] = new Dictionary<object, object>()
                {
                    ["read speed test"] = new Dictionary<object, object>()
                    {
						["close console"] = false,
						["read"] = 20,
						["write"] = 20,
                    }
                }
            };

            DateTime start;

			if (args.Length > 0 && File.Exists(args[0]))
			{
				using (var s = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64000, FileOptions.SequentialScan))
				{
					settings.Union(kJson.JSON.Parse(s) as Dictionary<object, object>);
				}
			}

			var profile = (settings["profiles"] as Dictionary<object, object>)[settings["working profile"]] as Dictionary<object, object>;
            int read_repeat = (int)profile["read"];
            int write_repeat = (int)profile["write"];

            object readresult = null;

			foreach (var fn in Directory.GetFiles(".", "*.json").Where(fn => !fn.Contains(".test.")))
			{
				try
				{
					if (read_repeat > 0)
					{
						using (var s = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64000, FileOptions.SequentialScan))
						{
							readresult = kJson.JSON.Parse(s);
						}
						start = DateTime.Now;
						for (int i = 0; i < read_repeat; i++)
						{
							using (var s = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64000, FileOptions.SequentialScan))
							{
								readresult = kJson.JSON.Parse(s);
							}

						}
						Console.WriteLine("'{0}' - read ok. Avg time - {1} ms", fn, (DateTime.Now - start).TotalMilliseconds / read_repeat);
					}
					if (write_repeat > 0)
					{
						start = DateTime.Now;
						for (int i = 0; i < read_repeat; i++)
						{
							using (var s = new FileStream(Path.GetFileName(fn) + ".test.json", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 64000))
							{
								kJson.JSON.Write(readresult, s);
							}

						}
						Console.WriteLine("'{0}' - write ok. Avg time - {1} ms", fn, (DateTime.Now - start).TotalMilliseconds / read_repeat);
					}
				}
				catch (Exception e)
				{
					if (e.InnerException != null)
					{
						Console.WriteLine("[{0}] \n\t {1}: {2}", fn, e.InnerException.GetType(), e.InnerException.Message);
					}
					else
					{
						Console.WriteLine("[{0}] \n\t {1}", fn, e);
					}

				}
			}

            if (!(bool)profile["close console"])
            {
                Console.WriteLine("Press any key to exit...");
                while (!Console.KeyAvailable) ;
            }
        }
    }
}
