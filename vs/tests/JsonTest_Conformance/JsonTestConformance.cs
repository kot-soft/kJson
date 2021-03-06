﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonTest_Conformance
{
	class JsonTestConformance
	{
		public static string PeekInput(kJson.Read.Parser context)
		{
            context.Input.Parse((int)kJson.Read.TokenType.Spaces);
            var c = context.Input.Peek();
            return c == kJson.Read.Reader.EOF ? "(EOF)" : "--> '" + c + '\'';
		}

		public static void Main(string[] args)
		{
            int filestested = 0;
            int errorsoccured = 0;
			kJson.Read.Parser parser = new kJson.Read.Parser()
            {

                Track = (context) =>
                {
                    string indent = string.Concat(Enumerable.Range(0, context.Stack.Count).Select(i => "\t"));
                    Console.WriteLine("{0} [{1}]: {2} {3}",
                        indent,
                        context.Stack.Top?.GetType().ToString().Split('`')[0].Split('.').Last(),
                        context.CurrentState,
                        PeekInput(context)
                        );
                },
                Error = (exception, context) =>
                {

                }
            };
            foreach (var fn in Directory.GetFiles(".", "*.json"))
			{
				foreach (var r in typeof(kJson.Read.RestrictLevel).GetEnumValues().Cast<kJson.Read.RestrictLevel>())
				{
					Console.WriteLine("");
					Console.WriteLine($"{fn} ({r})");
					Console.WriteLine("-----------------------------");
					try
					{
                        filestested++;
						using (var fs = new FileStream(fn, FileMode.Open))
						{
							var intermediate = parser.Parse(new kJson.Read.StreamReader(fs), kJson.Read.RestrictLevel.Tolerance);
							Console.WriteLine("=============================");
						}
					}
					catch (Exception e)
					{
                        errorsoccured++;
                        if (e.InnerException!=null)
						{
							Console.WriteLine("{0}: {1}", e.InnerException.GetType(), e.InnerException.Message);
						}
						else
						{
							Console.WriteLine(e);
						}

					}
				}

			}

            Console.WriteLine("-----------------------------");
            Console.WriteLine($"{filestested} tests passed. {errorsoccured} errors total occured.");
            while (!Console.KeyAvailable) ;
		}
	}
}
