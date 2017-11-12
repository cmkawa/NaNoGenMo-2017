﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HelloWorld
{
    public class Hello
    {
        private Dictionary<String, int> allWords = new Dictionary<String, int>();
        private Dictionary<String, Dictionary<String, int>> nextWords = new Dictionary<String, Dictionary<String, int>>();
        private int longestWord = 0;

        public static void Main()
        {
            Boolean repeat = true;
            String input = "";
            String fullInput;

            Hello hello = new Hello();

            while (repeat)
            {
                fullInput = "";

                Console.WriteLine("Enter text: (End input with line containing only CTRL + Z)");

                do
                {
                    input = Console.ReadLine();

                    fullInput += input + "\n";

                } while (input != null);

                int[] counts = hello.CountWords(fullInput);
                // 0: word count
                // 1: sentence count
                // 2: paragraph count

                hello.PrintWords(hello.longestWord);
                Console.WriteLine("Word count: " + counts[0]);

                if (counts[0] > 0)
                {
                    double wordsPerSentence = Math.Round((double)counts[0] / (double)counts[1], 3);
                    Console.WriteLine("Average words per sentence: " + wordsPerSentence);

                    double wordsPerParagraph = Math.Round((double)counts[0] / (double)counts[2], 3);
                    Console.WriteLine("Average words per paragraph: " + wordsPerParagraph);
                }
                
                Console.WriteLine("Again? y/n");

                input = Console.ReadLine();

                if (!input.Equals("y"))
                {
                    repeat = false;
                    Console.WriteLine("Goodbye!");
                }
                else
                {
                    hello.allWords.Clear();
                    hello.nextWords.Clear();    // Should this always happen?
                    Console.WriteLine("==========\n");
                }
            }

            System.Threading.Thread.Sleep(1500);
        }

        private int[] CountWords(String input)
        {
            List<double> sentences = new List<double>();
            List<double> paragraphs = new List<double>();
            List<double> chapters = new List<double>();

            int totalWordCount = 0;
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
                Boolean endParagraph = false;
                Boolean endChapter = true;

                for (int i = 0; i < input.Length; i++)
                {
                    if (IsStopChar(input[i]))
                    {
                        lastWord = foundWord;
                        foundWord = input.Substring(currWordStart, i - currWordStart);

                        if (foundWord.Length > 0)
                        {
                            //Console.WriteLine("Found: " + foundWord);
                            AddWord(lastWord, foundWord);
                            totalWordCount++;
                            wordCount++;

                            if (foundWord.Length > longestWord)
                            {
                                longestWord = foundWord.Length;
                            }

                            if (CheckEndOfSentence(foundWord))
                            {
                                //Console.WriteLine("Found at end of sentence: " + foundWord);
                                sentences.Add(wordCount);
                                wordCount = 0;
                                sentenceCount++;
                            }
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

                            while (input[i].Equals('\n'))
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

            return new int[] { totalWordCount, sentences.Count, paragraphs.Count };
        }

        private void AddWord(String word, String nextWord)
        {
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

            // TODO: Remove "" from allWords? Or use to mark new chapters.
        }

        private void PrintWords(int width)
        {
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
            Console.WriteLine("\nTop 20 Most Frequent Words:");

            var sortedWords = from pair in allWords
                              orderby pair.Value ascending
                              select pair;

            for (int i = 20; i > 0; i--)
            {
                KeyValuePair<String, int> pair = sortedWords.ElementAt(sortedWords.Count() - i);
                String print = String.Format("{0," + width + "}", pair.Key);
                Console.WriteLine(print + ": " + pair.Value);
            }
            */

            // Display next words after most frequent word.
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