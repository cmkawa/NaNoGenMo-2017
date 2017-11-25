using System;
using System.Collections.Generic;
using System.Linq;

namespace StrayCat
{
    public class FMASC
    {
        private struct Word
        {
            public int count;
            public Dictionary<String, int> nextWords;
            // Add information about word type.
            public Boolean isCapitalized;
            public Boolean isStartDialogue;
            public Boolean isEndDialogue;
            public int distanceToLastWord;
        }

        private Dictionary<String, Word> prediction = new Dictionary<String, Word>();
        private Dictionary<String, int> distances = new Dictionary<String, int>();
        // TODO: Fold this into existing code, attempt to utilize the Word struct.

        private Dictionary<String, int> allWords = new Dictionary<String, int>();
        private Dictionary<String, Dictionary<String, int>> nextWords = new Dictionary<String, Dictionary<String, int>>();
        private int longestWord = 0;
        private int totalCount = 0;

        public static void Main()
        {
            String input = "y";
            String fullInput;

            FMASC meow = new FMASC();

            while (input.Equals("y"))
            {
                fullInput = "";

                Console.WriteLine("==========\n");
                Console.WriteLine("Enter text: (End input with line containing only CTRL + Z)");

                do
                {
                    input = Console.ReadLine();

                    fullInput += input + "\n";

                } while (input != null);

                int[] counts = meow.CountWords(fullInput);
                // 0: word count
                // 1: sentence count
                // 2: paragraph count

                meow.PrintWords(meow.longestWord);
                Console.WriteLine("Word count: " + counts[0]);

                if (counts[0] > 0)
                {
                    double wordsPerSentence = Math.Round((double)counts[0] / (double)counts[1], 3);
                    Console.WriteLine("Average words per sentence: " + wordsPerSentence);

                    double wordsPerParagraph = Math.Round((double)counts[0] / (double)counts[2], 3);
                    Console.WriteLine("Average words per paragraph: " + wordsPerParagraph);
                }

                String genWords;
                int genCount = 0;
                meow.SortLists();
                meow.CountDistances();

                input = "y";

                while(input.Equals("y"))
                {
                    while (genCount == 0)
                    {
                        Console.WriteLine("How many words should be generated?");
                        genWords = Console.ReadLine();

                        if (!Int32.TryParse(genWords, out genCount))
                        {
                            Console.WriteLine("\nInvalid input provided.");
                        }
                    }

                    meow.GenerateText(genCount);

                    Console.WriteLine("Generate more? y/n");
                    input = Console.ReadLine();
                }
                
                Console.WriteLine("Submit next text? y/n");
                input = Console.ReadLine();
                
                meow.allWords.Clear();
                meow.nextWords.Clear();   
                // Should this always happen?
            }

            Console.WriteLine("Goodbye!");
            System.Threading.Thread.Sleep(1500);
        }

        private int[] CountWords(String input)
        {
            List<double> sentences = new List<double>();
            List<double> paragraphs = new List<double>();
            List<double> chapters = new List<double>();

            totalCount = 0;
            int wordCount = 0;
            int sentenceCount = 0;
            int paragraphCount = 0;

            if (input.Length > 0)
            {
                // ASSUME that the first line of input is a chapter header.

                int currWordStart = 0;
                String lastWord = "";
                String foundWord = "";
                longestWord = 0;

                Boolean lastCharNewline = false;
                Boolean endSentence = false;
                Boolean endParagraph = false;
                Boolean endChapter = true;

                for (int i = 0; i < input.Length; i++)
                {
                    if (IsStopChar(input[i]))
                    {
                        if (foundWord.Length > 0)
                        {
                            lastWord = foundWord;
                        }

                        foundWord = input.Substring(currWordStart, i - currWordStart);

                        if (foundWord.Length > 0)
                        {
                            totalCount++;
                            wordCount++;

                            if (foundWord.Length > longestWord)
                            {
                                longestWord = foundWord.Length;
                            }

                            if (CheckEndOfSentence(foundWord))
                            {
                                Console.WriteLine("Found at end of sentence: " + foundWord);
                                sentences.Add(wordCount);
                                wordCount = 0;
                                sentenceCount++;
                                endSentence = true;
                            }
                            else
                            {
                                endSentence = false;
                            }

                            //Console.WriteLine("Found: " + foundWord);
                            AddWord(lastWord, foundWord, endSentence);
                        }
                        else if (IsNewline(input[i]))
                        {
                            if (endParagraph)
                            {
                                if (paragraphCount > 0)
                                { 
                                    chapters.Add(paragraphCount);
                                    paragraphCount = 0;
                                }

                                endChapter = true;
                            }
                            else if (lastCharNewline)
                            {
                                if (wordCount > 0)
                                {
                                    // Sentence ended without typical end punctuation.
                                    //Console.WriteLine("Found at end of sentence: " + lastWord);
                                    sentences.Add(wordCount);
                                    wordCount = 0;
                                    sentenceCount++;
                                }
                                
                                if (sentenceCount > 0)
                                {
                                    //Console.WriteLine("End of paragraph!");
                                    paragraphs.Add(sentenceCount);
                                    sentenceCount = 0;
                                    paragraphCount++;
                                }

                                endParagraph = true;
                            }
                        }
                        
                        lastCharNewline = IsNewline(input[i]);
                        currWordStart = i + 1;
                    }
                    else
                    {
                        if (endChapter)
                        {
                            // Begin tracking chapter title.
                            currWordStart = i; 

                            // Skip ahead until newline.
                            while (!input[i].Equals('\n') && endChapter)
                            {
                                i++;

                                // Check for blank line after chapter title.
                                if (input[i].Equals('\n'))
                                {
                                    // Chapter title ends.
                                    endChapter = false;
                                }                  
                            }

                            // TODO: Collect chapter titles?
                            String chapterTitle = input.Substring(currWordStart, i - currWordStart);
                            Console.WriteLine("Found chapter title: " + chapterTitle);

                            while (i < input.Length && input[i].Equals('\n'))
                            {
                                // Advance through any additional blank lines after chapter title.
                                i++;
                            }

                            if (paragraphCount > 0)
                            {
                                chapters.Add(paragraphCount);
                                paragraphCount = 0;
                            }

                            // Resume normal processing after blank line after chapter title.
                            currWordStart = i;
                            continue;
                        }

                        lastCharNewline = false;
                        endParagraph = false;
                    }
                }

                // Include last word found.
                AddWord(lastWord, "", true);
                
                // Clean up empty strings.
                prediction.Remove("");
                distances.Remove("");
                prediction[lastWord].nextWords.Remove("");

                Word insert;

                // Update list of distances.
                foreach (KeyValuePair<String, int> dist in distances)
                {
                    Console.WriteLine("Adding {0}: {1}", dist.Key, dist.Value);
                    insert = prediction[dist.Key];
                    insert.distanceToLastWord = dist.Value;
                    prediction[dist.Key] = insert;
                }

                if (sentences.Count == 0 || wordCount != 0)
                {
                    sentences.Add(wordCount);
                    sentenceCount++;
                }

                if (paragraphs.Count == 0 || sentenceCount != 0)
                {
                    paragraphs.Add(sentenceCount);
                    paragraphCount++;
                }

                if (chapters.Count == 0 || paragraphCount != 0)
                {
                    chapters.Add(paragraphCount);
                }

                Console.WriteLine("Words per sentence:");
                CalculateStandardDeviation(sentences);

                Console.WriteLine("Sentences per paragraph:");
                CalculateStandardDeviation(paragraphs);

                Console.WriteLine("Paragraphs per chapter:");
                CalculateStandardDeviation(chapters);
            }

            return new int[] { totalCount, sentences.Count, paragraphs.Count };
        }

        private void AddWord(String word, String nextWord, Boolean endSentence)
        {
            // Mark II: Probability select, one dictionary with Word objects.
            if (prediction.TryGetValue(word, out Word wordStats))
            {
                // Word was found in the prediction dictionary
                wordStats.count += 1;
                prediction[word] = wordStats;

                if (wordStats.nextWords.TryGetValue(nextWord, out int nextCount))
                {
                    // Next word was found in the nextWords list
                    wordStats.nextWords[nextWord] = nextCount + 1;
                }
                else
                {
                    // Add new next-word
                    wordStats.nextWords.Add(nextWord, 1);
                }
            }
            else
            {
                // Initialize completely new entry in predictions
                wordStats.count = 1;
                wordStats.nextWords = new Dictionary<string, int> { { nextWord, 1 } };
                wordStats.distanceToLastWord = -1;
                prediction.Add(word, wordStats);
            }

            // Track words that end sentences.
            if (endSentence && !distances.ContainsKey(nextWord))
            {
                distances.Add(nextWord, 0);
            }

            // Mark I: Random select, two dictionaries.
            // Add word to allWords dictionary.
            if (allWords.TryGetValue(word, out int count))
            {
                allWords[word] = count + 1;
            }
            else
            {
                allWords.Add(word, 1);
            }

            // Add following word to nextWords list associated with preceding word.
            if (nextWords.TryGetValue(word, out Dictionary<String, int> nextList))
            {
                if (nextList.TryGetValue(nextWord, out count))
                {
                    // Next word has been seen to follow first.
                    nextList[nextWord] = count + 1;

                    // Debugging
                    /*
                    if (word.Equals("the"))
                    {
                        Console.WriteLine(">>>Found \"{0} {1}\" again, occurrence {2}", word, nextWord, nextList[nextWord]);
                    }
                    */
                }
                else
                {
                    nextList.Add(nextWord, 1);

                    // Debugging
                    /*
                    if (word.Equals("the"))
                    {
                        Console.WriteLine(">>>Found \"{0} {1}\" the first time.", word, nextWord);
                    }
                    */
                }
            }
            else
            {
                nextList = new Dictionary<string, int> { { nextWord, 1 } };
                nextWords.Add(word, nextList);
            }
            // TODO: Use to mark new chapters?
        }

        private void PrintWords(int width)
        {
            // Inspect distances dictionary.
            Console.WriteLine("There are {0} words with distance 0.", distances.Count);

            foreach (KeyValuePair<String, Word> item in prediction)
            {
                Console.WriteLine(item.Key + " => " + item.Value.distanceToLastWord);
            }

            // Compare Mark II to Mark I.
            /*
            Console.WriteLine("Found " + prediction.Count + " different words!");
            Console.WriteLine("Mark I found " + allWords.Count + " words.");
            
            Console.WriteLine("Prediction top 20 words:");

            var sortedWords = from pair in prediction
                              orderby pair.Value.count ascending
                              select pair;

            for (int i = 20; i > 0; i--)
            {
                KeyValuePair<String, Word> pair = sortedWords.ElementAt(sortedWords.Count() - i);
                String print = String.Format("{0," + width + "}", pair.Key);
                Console.WriteLine(print + ": " + pair.Value.count);
            }

            Console.WriteLine("AllWords top 20 words:");

            var sortedWords2 = from pair in allWords
                              orderby pair.Value ascending
                              select pair;

            for (int i = 20; i > 0; i--)
            {
                KeyValuePair<String, int> pair = sortedWords2.ElementAt(sortedWords2.Count() - i);
                String print = String.Format("{0," + width + "}", pair.Key);
                Console.WriteLine(print + ": " + pair.Value);
            }

            Console.WriteLine("Prediction found the following {0} words after \"and\":", prediction["and"].nextWords.Count);

            foreach (KeyValuePair<String, int> next in prediction["and"].nextWords)
            {
                Console.WriteLine(">>{0} with count {1}", next.Key, next.Value);
            }

            Console.WriteLine("AllWords found the following {0} words after \"and\":", nextWords["and"].Count);

            foreach (KeyValuePair<String, int> next2 in nextWords["and"])
            {
                Console.WriteLine(">>{0} with count {1}", next2.Key, next2.Value);
            }
            */

            // Random display.
            /*
            Console.WriteLine("Found " + allWords.Count() + " different words!");
            Console.WriteLine("The longest was " + width + " words long.");

            Random numGen = new Random();

            int getIndex = numGen.Next(0, allWords.Count());
            KeyValuePair<String, int> fetchWord = allWords.ElementAt(getIndex);

            Console.WriteLine("One of them was " + fetchWord.Key + ", which showed up " + fetchWord.Value + " times.");

            getIndex = numGen.Next(0, allWords.Count());
            fetchWord = allWords.ElementAt(getIndex);

            Console.WriteLine("Another was " + fetchWord.Key + ", which showed up " + fetchWord.Value + " times.");
            */

            // High Frequency display.
            /*
            int top = 50;
            Console.WriteLine("\nTop {0} Most Frequent Words:", top);

            var sortedWords = from pair in prediction
                              orderby pair.Value.count ascending
                              select pair;

            for (int i = top; i > 0; i--)
            {
                KeyValuePair<String, Word> pair = sortedWords.ElementAt(sortedWords.Count() - i);
                String print = String.Format("{0," + width + "}", pair.Key);
                Console.WriteLine(print + ": " + pair.Value.count);
            }
            */

            // Display next words after most frequent word.
            /*
            if (allWords.Count > 0)
            {
                var sortedWords = from pair in allWords
                                  orderby pair.Value descending
                                  select pair;

                KeyValuePair<String, int> topWord = sortedWords.ElementAt(0);

                Dictionary<String, int> getNext = nextWords[topWord.Key];

                var sortedNext = from pair in getNext
                                 orderby pair.Value descending
                                 select pair;

                Console.WriteLine("\nMost common word (" + topWord.Value + " occurrences) was: " + topWord.Key);
                Console.WriteLine("Next words were:");

                foreach (KeyValuePair<String, int> item in sortedNext)
                {
                    Console.WriteLine("  " + item.Key + ": " + item.Value);
                }
            }
            */
        }

        private void SortLists()
        {
            allWords = allWords.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            Dictionary<String, Dictionary<String, int>> placeholder = new Dictionary<string, Dictionary<string, int>>();
                        
            foreach (KeyValuePair<String, Dictionary<String, int>> item in nextWords)
            {
                placeholder.Add(item.Key, item.Value.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value));
            }

            nextWords = placeholder;

            prediction = prediction.OrderByDescending(pair => pair.Value.count).ToDictionary(pair => pair.Key, pair => pair.Value);

            Dictionary<String, Word> tempPrediction = new Dictionary<string, Word>();
            Dictionary<String, int> tempDict = new Dictionary<String, int>();
            Word tempWord;

            foreach (KeyValuePair<String, Word> dict in prediction)
            {
                tempDict = dict.Value.nextWords.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                tempWord = dict.Value;
                tempWord.nextWords = tempDict;
                tempPrediction.Add(dict.Key, tempWord);
            }

            prediction = tempPrediction;
        }

        private void CountDistances()
        {
            // Dictionary distances begins only with words that end sentences (distance = 0).
            // While distances does not have the same number of members as predictions, keep searching.
            //   Start from index 0 of distances and go up. ---> Can distances ever run out of words first?
            //   Check all words with distance == -1 (unmarked) for distances[index] in nextWords list.
            //     If found, add entry to distances at distance + 1.

            Console.WriteLine("Counting distances...");

            int index = 0;

            while(index < distances.Count)
            {
                Console.WriteLine("Checking {0}: {1}", distances.ElementAt(index).Key, distances.ElementAt(index).Value);

                index++;
            }
        }

        private void GenerateTextOriginal(int count)
        {
            String generated = "";
            Random rand = new Random();

            // For now, select a random word.
            String gotWord = allWords.ElementAt(rand.Next(allWords.Count)).Key;
            
            String nextGeneratedWord = "";
            generated = gotWord;

            for (int i = 1; i < count; i++)
            {
                if(nextWords.TryGetValue(gotWord, out Dictionary<String, int> nextList))
                {
                    nextGeneratedWord = nextList.ElementAt(rand.Next(nextWords[gotWord].Count)).Key;
                }
                else
                {
                    nextGeneratedWord = allWords.ElementAt(rand.Next(allWords.Count)).Key;
                }

                generated += " " + nextGeneratedWord;

                gotWord = nextGeneratedWord;
            }

            Console.WriteLine(generated);
        }

        private void GenerateText(int count)
        {
            String generated = "";
            Random rand = new Random();

            // For now, select a random word.
            //String gotWord = allWords.ElementAt(rand.Next(allWords.Count)).Key;
            int selectWord = rand.Next(totalCount);

            Console.WriteLine(">>Rolled a {0} out of {1}.", selectWord, totalCount);

            int index = 0;
            int target = 0;

            while (selectWord > target)
            {
                // Wrap around back to first element.
                if (index > prediction.Count)
                {
                    index = 0;
                }

                target += prediction.ElementAt(index).Value.count;
                index++;
            }

            // Step back to last word that captured the random roll.
            index--;
            if (index < 0)
            {
                index = prediction.Count - 1;
            }

            String gotWord = prediction.ElementAt(index).Key;
            Console.WriteLine("{0} [{1}]", gotWord, index);
            // Eventually, check if a word is a start word before starting with it.

            String nextGeneratedWord = "";
            generated = gotWord;

            for (int i = 1; i < count; i++)
            {
                /* allWords + nextWords version
                if(nextWords.TryGetValue(gotWord, out Dictionary<String, int> nextList))
                {
                    nextGeneratedWord = nextList.ElementAt(rand.Next(nextWords[gotWord].Count)).Key;
                }
                else
                {
                    nextGeneratedWord = allWords.ElementAt(rand.Next(allWords.Count)).Key;
                }
                */

                if (prediction.TryGetValue(gotWord, out Word wordStats) && wordStats.nextWords.Count > 0)
                {
                    selectWord = rand.Next(wordStats.count);
                    Console.Write(">>Rolled a {0} out of {1}: ", selectWord, wordStats.count);
                    index = 0;
                    target = 0;

                    while (selectWord > target)
                    {
                        // Wrap around back to first element.
                        if (index > wordStats.nextWords.Count)
                        {
                            index = 0;
                        }

                        target += wordStats.nextWords.ElementAt(index).Value;
                        index++;
                    }

                    // Step back to last word that captured the random roll.
                    index--;
                    if (index < 0)
                    {
                        index = wordStats.nextWords.Count - 1;
                    }

                    nextGeneratedWord = wordStats.nextWords.ElementAt(index).Key;
                    Console.WriteLine("{0} [{1}]", nextGeneratedWord, index);
                }
                else
                {
                    // Choose a random (unweighted) word from the first column.
                    nextGeneratedWord = prediction.ElementAt(rand.Next(prediction.Count)).Key;
                    Console.WriteLine(">>RANDOM " + nextGeneratedWord);
                }
                
                generated += " " + nextGeneratedWord;

                gotWord = nextGeneratedWord;
            }

            Console.WriteLine(generated);
        }

        static int[] CountWordsOriginal(String input)
        {
            List<double> sentences = new List<double>();
            List<double> paragraphs = new List<double>();
            List<double> chapters = new List<double>();

            int totalWordCount = 0;
            int wordsInSentence = 0;
            int sentencesInParagraph = 0;
            int paragraphsInChapter = 0;

            if (input.Length > 0)
            {
                // Always consists of at least one sentence/paragraph/chapter.

                int lastWordIndex = 0;
                Boolean lastWhitespace = true;
                String foundWord = "";
                Boolean endParagraph = true;
                Boolean endChapter = true;

                for (int i = 0; i < input.Length; i++)
                {
                    if (CheckWhitespace(input[i]))
                    {
                        if (endParagraph)
                        {
                            // Two or more consecutive blank lines, end of chapter.
                            endChapter = true;
                            chapters.Add(paragraphsInChapter);

                            continue;
                        }

                        if (lastWhitespace)
                        {
                            // Consecutive whitespace chars, keep looking.
                            lastWordIndex = i + 1;

                            if (!endParagraph && input[i].Equals('\n') && input[i - 1].Equals('\n'))
                            {
                                // New paragraph after two consecutive newline chars.
                                endParagraph = true;
                                paragraphs.Add(sentencesInParagraph);
                            }

                            continue;
                        }
                        
                        foundWord = input.Substring(lastWordIndex, i - lastWordIndex);

                        if (!foundWord.Contains('\n'))
                        {
                            // Complete word in actual sentence found.
                            totalWordCount++;
                            wordsInSentence++;

                            Console.WriteLine("Found \"" + foundWord + "\"");
                        }

                        if (CheckEndOfSentence(foundWord))
                        {
                            if (wordsInSentence != 0)
                            {
                                sentences.Add(wordsInSentence);
                                sentencesInParagraph++;
                            }

                            wordsInSentence = 0;
                        }

                        lastWordIndex = i + 1;
                        lastWhitespace = true;
                    }
                    else
                    {
                        lastWhitespace = false;

                        if (endChapter)
                        {
                            // Chapter title.
                            // TODO: Handle chapter titles as unique word prediction case.
                            while (input[i] != '\n')
                            {
                                // For now, ignore chapter titles.
                                i++;
                            }

                            paragraphsInChapter = 0;
                            endChapter = false;
                            endParagraph = false;
                        }

                        if (endParagraph)
                        {
                            // Consecutive newline chars followed by non-whitespace.
                            sentencesInParagraph = 0;
                            endParagraph = false;
                            paragraphsInChapter++;
                        }
                    }
                }
                
                // Single line that did not contain end punctuation.
                if (sentences.Count == 0)
                {
                    sentences.Add(totalWordCount);
                }

                if (paragraphs.Count == 0)
                {
                    paragraphs.Add(sentencesInParagraph);
                }

                if (chapters.Count == 0)
                {
                    chapters.Add(paragraphsInChapter);
                }
            }

            Console.WriteLine("Words per sentence:");
            CalculateStandardDeviation(sentences);

            Console.WriteLine("Sentences per paragraph:");
            CalculateStandardDeviation(paragraphs);

            Console.WriteLine("Paragraphs per chapter:");
            CalculateStandardDeviation(chapters);

            return new int[] { totalWordCount, sentences.Count, paragraphs.Count, chapters.Count };
        }

        static double CalculateStandardDeviation(List<double> numList)
        {
            double stdDev = 0;

            if (numList.Count > 0)
            {
                double average = numList.Average();
                double avgDiff = 0;

                foreach (double item in numList)
                {
                    //Console.WriteLine(item);
                    avgDiff += Math.Pow((item - average), 2);
                }

                double variance = avgDiff / numList.Count;
                stdDev = Math.Sqrt(variance);

                Console.WriteLine(" Mean Average: " + Math.Round(average, 2));
                Console.WriteLine(" Standard Deviation: " + Math.Round(stdDev, 2));
            }
            
            return stdDev;
        }

        static Boolean CheckWhitespace(char c)
        {
            char[] whitespace = new char[] { ' ', '\t', '\n' };

            return Array.Exists(whitespace, element => element == c);
        }

        static Boolean IsStopChar(char c)
        {
            // ASSUME no tabs in txt file.
            return c.Equals(' ') || c.Equals('\n');
        }

        static Boolean IsNewline(char c)
        {
            return c.Equals('\n');
        }

        static Boolean CheckEndOfSentence(String word)
        {
            char[] endSentence = new char[] { '.', '!', '?' };
            String[] exceptions = new String[] { "Mrs.", "Mr.", "Ms.", "Dr." };

            Boolean endPunctuation = Array.Exists(endSentence, element => element == word[word.Length - 1]);
            //Boolean endQuotation = word[word.Length - 1].Equals('\"') && Array.Exists(endSentence, element => element == word[word.Length - 2]);
            Boolean isException = Array.Exists(exceptions, element => word.EndsWith(element));

            //return (endPunctuation || endQuotation) && !isException;

            return endPunctuation && !isException;
        }
    }
}