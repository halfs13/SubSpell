
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace SubSpell
{
	class Checker
    {
		//number of first characters
		private int dictCats;
		//holds the categories
        public Dictionary<char, Dictionary<string,int>> dict;
		private Dictionary<string, string> changes = new Dictionary<string, string>();
		private int totalWords;

        public Checker()
        {
			//load the dictionary from file
            loadDic();

			//load the change all fixes file
			loadChageAll();
		}

        private bool loadDic()
        {
            TextReader dr = new StreamReader("dic.ssdic");
            string line;

			dictCats = Int32.Parse(dr.ReadLine());
			totalWords = Int32.Parse(dr.ReadLine());
            dict = new Dictionary<char, Dictionary<string,int>>(dictCats);
            for (int i = 0; i < dictCats; i++)
            {
                line = dr.ReadLine();
                parseCat(line);
            }
            dr.Close();
			
            return true;
        }

		private void parseCat(string pline)
		{
			//break the cat string on spaces
			string[] raline = pline.Split(' ');
			//dict to hold all the words & counts
			Dictionary<string,int> words = new Dictionary<string,int>();
			if (Int32.Parse(raline[1]) != 0)
			{
				int i = 2;
				while (i < raline.Length)
				{
					words[raline[i]] = Int32.Parse(raline[i + 1]);
					i = i + 2;
				}
			}

			dict.Add(raline[0][0], words);
		}

		private void loadChageAll()
		{
			TextReader cr = new StreamReader("chg.ssdic");
			string ln;
			string[] parts;
			while ((ln = cr.ReadLine()) != null)
			{
				//Console.WriteLine(ln);
				parts = ln.Split(' ');
				//Console.WriteLine(parts[0] + " " + parts[1]);
				changes.Add(parts[0], parts[1]);
			}
			cr.Close();
		}

		//loads the file and starts checking
        public bool loadFile(string fname)
        {
			try
			{
				//openfile
				TextReader tr = new StreamReader(fname);
				//fix numbers
				fixNumbers(tr);
				//close
				tr.Close();
				//copy back
				TextWriter tw = new StreamWriter(fname);
				copyTempFile(tw);
				tw.Close();
				//Done Number Fix
				Console.WriteLine("\n\n-------------------------------------------");
				Console.WriteLine(" Done Number Fix");
				Console.WriteLine("-------------------------------------------\n\n");
				
				//@todo
				//fix punctuations?
				//.. other than ...
				//.? or ?.
				//:. or .:
				//.! or !.
				//;: or :; or .; or ;.
				//., or ,.

				//open file
				tr = new StreamReader(fname);
				//checkfile
				bool rresult = checkFile(tr);
				//closefile
				tr.Close();

				//write temp back to orig
				tw = new StreamWriter(fname);
				bool wresult = copyTempFile(tw);
				tw.Close();
				//blank temp file to clear space
				tw = new StreamWriter(".subspell.temp");
				tw.Close();

				writeDict();

				return true;
			}
			catch (IOException)
			{
				return false;
			}
        }

		private void fixNumbers(TextReader tr)
		{
			TextWriter tw = new StreamWriter(".subspell.temp");
			string line = tr.ReadLine();
			
			while (line != null)
			{				
				line = Regex.Replace(line, @"(\d) (\d)", @"$1$2");
				Console.WriteLine(line);
				tw.WriteLine(line);
				line = tr.ReadLine();
			}

			tw.Close();
		}

		private bool copyTempFile(TextWriter tw)
		{
			try
			{
				TextReader tr = new StreamReader(".subspell.temp");
				string text = tr.ReadLine();
				while (text != null)
				{
					tw.WriteLine(text);
					text = tr.ReadLine();
				}
				tr.Close();
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		//@TODO
        private bool checkFile(TextReader fr)
        {
			TextWriter tw = new StreamWriter(".subspell.temp");
			string line = fr.ReadLine();
			string newline = "";
			bool endI = false;

			while (line != null)
			{
				newline = "";

				//skip srt format lines
				if (line.CompareTo("") == 0 || Regex.IsMatch(line, @"^[\d]*$") || Regex.IsMatch(line, @"\d\d:\d\d:\d\d"))
				{
					newline = line;
				}
				else if(line.StartsWith("<i>",StringComparison.OrdinalIgnoreCase) == true)//if begins <i> 
				{
					//strip all <i> and </i>
					line = line.Replace("<i>","");
					line = line.Replace("<I>","");

					if (line.EndsWith(@"</i>", StringComparison.OrdinalIgnoreCase) == true)
					{
						endI = true;
						line = line.Replace(@"</I>", "");
						line = line.Replace(@"</i>", "");
					}
					//check line
					newline = checkLine(line);

					//prepend <i> append </i>
					if (endI == true)
					{
						newline = "<i>" + newline + @"</i>";
						endI = false;
					}
					else
					{
						newline = "<i>" + newline;
					}
					//write to file
				}
				else
				{
					newline = checkLine(line);
				}

				//Console.WriteLine("\n" + line);
				Console.WriteLine(">>>>>>" + newline);
				tw.WriteLine(newline);
				line = fr.ReadLine();
			}

			tw.Close();
			return true;
        }

		//CheckLine(string)
		///<summary>
		///Checks the provided line against the dictionary after stripping 
		///leading and trailing italics tags. In the event of a word found not
		///in the dictionary, the user is prompted for correction with the
		///option of \\ for no correction to the given word.
		///</summary>
		/// 
		///<param name="line">
		///string - the line to be checked
		///</param>
		///
		///<returns>
		///string - the checked/corrected string
		///</returns>
        private string checkLine(string line)
        {
            string[] words = line.Split(' ');
			Console.WriteLine(words.Length);
			string newline = "";
			int i = 0;
            while(i < words.Length)
            {
				try
				{
					totalWords++;
					//Console.WriteLine(words[i]);
					if (!dict[words[i][0]].ContainsKey(words[i]) && !changes.ContainsKey(words[i]))
					{
						if (dict[words[i].Replace('l', 'I')[0]].ContainsKey(words[i].Replace('l', 'I')))
						{
							newline = newline + words[i].Replace('l', 'I') + " ";
						}
						else
						{
							Console.WriteLine("\nPlease Verify:");
							Console.WriteLine("Line: " + String.Join(" ", words));
							Console.WriteLine("Word: " + words[i]);
							Console.WriteLine("------------------------");
							Console.WriteLine("Possibles:");
							string[] temp = getRecs(words[i], 5);
							if (temp != null)
							{
								Console.WriteLine(String.Join(" ", getRecs(words[i], 5)));
							}
							else
							{
								Console.WriteLine("none");
							}
							Console.WriteLine("------------------------");
							string newWord = Console.ReadLine();
							
							
							if (newWord.CompareTo(@"\\\") == 0)
							{
								Console.WriteLine("\n ==Command Mode==");

								while (true)
								{
									Console.WriteLine("Enter command:");
									newWord = Console.ReadLine();

									try
									{
										if (newWord.CompareTo("save and quit") == 0)
										{
											writeDict();
											Environment.Exit(0);
										}
										else if (newWord.CompareTo("delete") == 0)
										{
											Console.WriteLine("Word to delete:");
											newWord = Console.ReadLine();
											Console.WriteLine(dict[newWord[0]].Remove(newWord));
										}
										else if (newWord.CompareTo("set change") == 0)
										{
											Console.WriteLine("Add or remove?");
											newWord = Console.ReadLine();
											if (newWord.CompareTo("add") == 0)
											{
												Console.WriteLine("From:");
												string from = Console.ReadLine();
												Console.WriteLine("To:");
												string to = Console.ReadLine();
												changes.Add(from, to);
											}
											else
											{
												Console.WriteLine("From word to delete:");
												newWord = Console.ReadLine();
												changes.Remove(newWord);
											}
										}
										else if (newWord.CompareTo("save") == 0)
										{
											writeDict();
										}
										else if (newWord.CompareTo("accept all") == 0)
										{
											int j = 0;
											foreach(string word in words)
											{
												if(j < i)
												{
													j++;
												}
												else if(!dict[word[0]].ContainsKey(word))
												{
													dict[word[0]].Add(word,0);
												}
											}
											i--;
										}
										else if (newWord.CompareTo("back") == 0)
										{
											break;
										}
									}
									catch
									{

									}
								}

							}

							if (newWord.CompareTo("") != 0 && newWord.CompareTo(@"\\") != 0)
							{
								if (!dict[newWord[0]].ContainsKey(newWord))
								{
									dict[newWord[0]].Add(newWord, 1);
								}
								newline = newline + newWord + " ";
								changes.Add(words[i], newWord);
							}
							else if (newWord.CompareTo(@"\\") == 0)
							{
								dict[words[i][0]].Add(words[i], 1);
								newline = newline + words[i] + " ";
							}
							else
							{
								while (true)
								{
									Console.WriteLine("Are you sure, remove " + words[i]);
									string answer = Console.ReadLine();
									if (answer.CompareTo("y") == 0)
									{
										break;
									}
									else if (answer.CompareTo("n") == 0)
									{
										Console.WriteLine("Replace with what?");
										newline = newline + Console.ReadLine() + " ";
										break;
									}
								}
							}
						}
					}
					else if (changes.ContainsKey(words[i]))
					{
						newline = newline + changes[words[i]] + " ";
						dict[changes[words[i]][0]][changes[words[i]]]++;
					}
					else
					{
						newline = newline + words[i] + " ";
						dict[words[i][0]][words[i]]++;
					}
				}
				catch (Exception)
				{

				}
				finally
				{
					i++;
				}
            }

			return newline.TrimEnd(' ');
        }

        private void writeDict()
        {
            //remove space from breaking dictionary
			dict.Remove(' ');
            //openfile
            TextWriter dw = new StreamWriter("dic.ssdic");
            //write dict.Count;
            dw.WriteLine(dictCats);
			dw.WriteLine(totalWords);
            string line;
            foreach (KeyValuePair<char, Dictionary<string,int>> entry in dict)
            {
				line = entry.Key + " " + entry.Value.Count;

                //while list != empty
                //write(list.get)
				foreach (KeyValuePair<string, int> word in entry.Value)
                {
					line = line + " " + word.Key + " " + word.Value;
                }
				dw.WriteLine(line);
            }
			dw.Close();
        }

		private string[] getRecs(string word, int num)
		{
			//@todo
			List<string> poss = findPossibles(word);
			if (poss != null)
			{
				Dictionary<string, double> recs = new Dictionary<string, double>();
				foreach (string w in poss) //check all the possibles for the n most likely
				{
					if (recs.Count < num)
					{
						recs[w] = calcProb(w);
					}
					else
					{
						double prob = calcProb(w);
						double i = 2.0;
						string key = null;
						bool exchange = false;

						foreach (KeyValuePair<string, double> r in recs) //test each in recomended list for lower prob than current
						{
							if (prob > r.Value)
							{
								exchange = true;
								if (r.Value < i)
								{
									i = r.Value;
									key = r.Key;
								}
							}
						}

						if (exchange == true) //if lower prob found then replace the lowest prob with the current
						{
							recs.Remove(key);
							recs[w] = prob;
						}
					}
				}

				string[] ret = new string[recs.Count];
				int j = 0;
				foreach (KeyValuePair<string, double> items in recs)
				{
					ret[j] = items.Key;
					j++;
				}
				return ret;
			}
			else
			{
				return null;
			}
		}

		private List<string> findPossibles(string word)
		{
			//@todo
			int i;
			string testWord;
			List<string> possibles = new List<string>(0);
			
			//1 letter off any char replaced with any other char
			IEnumerable<char> chars = dict.Keys;
			for (i = 0; i < word.Length; i++)
			{
				foreach (char c in chars)
				{
					testWord = word.Remove(i, 1);
					testWord = testWord.Insert(i, c.ToString());
					
					if (dict[testWord[0]].ContainsKey(testWord))
					{
						
						possibles.Add(testWord);
					}
				}
			}
			
			//1 shorter
			for (i = 0; i < word.Length; i++)
			{
				
				testWord = word.Remove(i, 1);

				if (dict[testWord[0]].ContainsKey(testWord))
				{
					possibles.Add(testWord);
				}
			}

			//1 longer of any char
			for (i = 0; i <= word.Length; i++)
			{
				foreach (char c in chars)
				{
					testWord = word.Insert(i, c.ToString());
					if (dict[testWord[0]].ContainsKey(testWord))
					{
						possibles.Add(testWord);
					}
				}
			}

			//@todo
			//possible implementation of swaps?
			//	for each in dic that contains letter 1:
			//		for each in dic that contains letter 2: ... ... until length

			//@debug Console.WriteLine("Count = " + possibles.Count);
			if (possibles.Count == 0)
			{
				return null;
			}
			else
			{
				return possibles;
			}
		}

		//checks the prob of a word vs total number of checked words
		//will error if the word doesnt exist?
		private double calcProb(string word)
		{
			int countUsed = dict[word[0]][word];
			return countUsed / totalWords;
		}
    }
}
