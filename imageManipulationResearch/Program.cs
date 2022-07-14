
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;


namespace imageManipulationResearch
{
    internal class Program
    {

        /// <summary>
        /// Outputs image file that is a copy of the input file but having pixels on the y axis shifted by offset (byPix)
        /// </summary>
        /// <param name="inputPath">Path to the input Image</param>
        /// <param name="outputPath">Path to where the output Image should be</param>
        /// <param name="byPix">Y offset in pixels</param>
        public static void ShiftY(string inputPath, string outputPath, int byPix)
        {
            
            
            // Load image from path
            using (Image<Rgba32> image = Image.Load<Rgba32>(inputPath))
            {
                // Create new image to save offset to
                using (Image<Rgba32> newImage = new Image<Rgba32>(image.Width, image.Width, new Rgba32(255, 255, 255)))
                {
                    
                    // Loop over every pixel
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            // Look for a non-white pixel (given input image is binary black and white, #000000FF or #FFFFFFFF)
                            if (image[x, y].R != 255)
                            {
                                // Write black pixel at new offset in new image (guarded against out of bounds issues)
                                Rgba32 blackPix = new Rgba32(0, 0, 0);
                                int newY = y + byPix;
                                if (0 <= newY && newY < image.Height)
                                {
                                    newImage[x, newY] = blackPix;
                                }
                                
                            }
                        }
                    }
                    
                    // Output offsetImage
                    newImage.Save(outputPath);
                }
            }
        }

        /// <summary>
        /// Applies a random y axis offset within range to each image in a folder and outputs them to a new folder with a descriptive suffix
        /// </summary>
        /// <param name="pathToFolder">Folder that needs to be processed</param>
        /// <param name="pixelRange">range from which a random offset will be chosen (currentY +(-pixelRange GT n LT pixelrange))</param>
        /// <returns></returns>
        public static string RandomOffsetFolder(string pathToFolder, int pixelRange)
        {
            Random r = new Random();
            
            
            string parentFolder = Directory.GetParent(pathToFolder).FullName;
            string newFolderName = new DirectoryInfo(pathToFolder).Name + "_yRandomized_"+ pixelRange +"_pix";
            string outputFolder = Path.Combine(parentFolder, newFolderName);

            // return a preShifted folder if output already exists
            if (Directory.Exists(outputFolder))
            {
                return outputFolder;
            }
            
            Console.WriteLine(outputFolder);
            // Create new output folder (if it doesn't exist)
            Directory.CreateDirectory(outputFolder);
            
            // Get array of all files in input folder
            FileInfo[] files = new DirectoryInfo(pathToFolder).GetFiles();

            // Loop over all the files in input folder and process them
            List<Thread> threads = new List<Thread>(); 
            foreach (FileInfo file in files)
            {   
                
                
                
                // Wait until a processor is theoretically available to take on a task (this semi-optimization guards against memory OutOfMemoryException)
                while (threads.Count >= Environment.ProcessorCount - 1)
                {
                    
                    // Get all the threads that are currently done
                    List<Thread> doneThreads = new List<Thread>();
                    foreach (Thread checkedThread in threads)
                    {
                        if (!checkedThread.IsAlive)
                        {
                            doneThreads.Add(checkedThread);
                        }   
                    }

                    // Join them and remove them
                    foreach (Thread doneThread in doneThreads)
                    {
                        doneThread.Join();
                        threads.Remove(doneThread);
                        Console.WriteLine("Removed done thread: " + doneThread.Name);
                    }
                }
                
                // Add new thread to the list
                string newFileName = Path.Combine(outputFolder, file.Name);
                Thread t = new Thread(() =>
                {
                    ShiftY(file.FullName, newFileName, r.Next(-pixelRange, pixelRange+1));
                });
                t.Name = file.Name;
                threads.Add(t);
                t.Start();
                
                Console.WriteLine("Processing " + file.Name + " started");
            }
            Console.WriteLine("Finished processing " + pathToFolder);
            
            // return output folder as new targetfolder
            return outputFolder;
        }
        

        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            RandomOffsetFolder(@"C:\Users\kevin\RiderProjects\imageManipulationResearch\imageManipulationResearch\exampleImageFolder", 11);
            stopWatch.Stop();
            
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }
    }
}