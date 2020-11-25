using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public enum ImageType
{
    BGRA, DEPTH
}

/// <summary>
/// Utility class to convert image related data into python readable format
/// </summary>
public class ImageConverter
{

    private static int numValuesPerPixelInRGBA = 4;
    private static int numValuesPerPixelInRGB = 3;

    /// <summary>
    /// Convert image singlethreaded
    /// </summary>
    /// <param name="image">image to convert</param>
    /// <param name="type">type of image</param>
    /// <param name="cameraResolution">image resolution</param>
    /// <returns></returns>
    public static byte[] ConvertImageToPython(byte[] image, ImageType type, CameraResolution cameraResolution)
    {
        Debug.Log("Converting Image");
        StringBuilder data = new StringBuilder();

        if (type == ImageType.BGRA)
        {
            data.Append("i[[");
        }
        else
        {
            data.Append("d[[");
        }
        int rowCounter = 0;
        for (int i = 0; i < image.Length; i += 4)
        {
            if (type == ImageType.DEPTH)
            {
                data.Append("[" + image[i] + "]");
            }
            else
            {
                data.Append("[" + image[i + 2].ToString() + "," + image[i + 1].ToString() + "," + image[i].ToString() + /*", " + image[i + 3].ToString() +*/ "],");
            }

            if (i > 4 && (i / 4) % cameraResolution.width == cameraResolution.width - 1)
            {
                // end of pixel line
                if (i + 3 < image.Length - 1)
                {
                    data.Append("],[");
                }
                // end of image
                else
                {
                    data.Append("],");
                }
                Debug.Log("Finished Row: " + rowCounter);
                rowCounter++;
            }
        }
        if (type == ImageType.BGRA)
        {
            data.Append("]");
        }
        else
        {
            data.Append("]");
        }
        data.Append(";");
        Debug.Log("Image Conversion Done");
        string img = data.ToString();

        byte[] imgByte = Encoding.Default.GetBytes(img);

        return imgByte;
    }

    /// <summary>
    /// Converts the given image in parallel using imageHeight jobs to calculate the result.
    /// </summary>
    /// <param name="image">the image to convert</param>
    /// <param name="type">type of the image</param>
    /// <param name="cameraResolution">resolution of the image</param>
    /// <returns></returns>
    public static byte[] ConvertImageToPythonParallel(byte[] image, ImageType type, CameraResolution cameraResolution)
    {
        int numJobs = cameraResolution.height;
        int numInputValuesPerJob = (image.Length - (image.Length / numValuesPerPixelInRGBA)) / numJobs;

        List<JobHandle> jobHandles = new List<JobHandle>();
        int jobIDCounter = 0;

        // collect all intermediate results for ease of access later on
        NativeArray<byte>[] values = new NativeArray<byte>[numJobs];

        // indicates which index in the input image we have to look
        int rgbaCounter = 0;
        int totalOutputLength = 0;
        int[] intermediateOutputLength = new int[numJobs];

        // create jobs to convert each image row individually
        for (int i = 0; i < numJobs; i++)
        {
            // container for input values of this job, has to be disposed manually (solved by annotation in ConversionJob)
            NativeArray<byte> tmpValues = new NativeArray<byte>(numInputValuesPerJob, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // output length estimates how many bytes are required to contain the converted image row
            // initial value of 3: [];
            int outputLength = 3;

            // we drop the alpha channel to reduce number of bytes to process and send
            for (int j = 0; j < tmpValues.Length; j += 3)
            {
                // take values for r,g,b channels
                byte b = image[rgbaCounter];
                byte g = image[rgbaCounter + 1];
                byte r = image[rgbaCounter + 2];
                tmpValues[j] = r;
                tmpValues[j + 1] = g;
                tmpValues[j + 2] = b;
                
                // calculate amount of bytes necessary for conversion, 5: [,,],
                outputLength += b.ToString().Length + g.ToString().Length + r.ToString().Length + 5;

                // skip alpha channel
                rgbaCounter += 4;
            }
            values[i] = tmpValues;
            // will contain converted output of each job, disposed later
            totalOutputLength += outputLength;
            intermediateOutputLength[i] = outputLength;
            jobIDCounter++;
        }
        
        NativeArray<byte> result = new NativeArray<byte>(totalOutputLength, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        int start = 0;
        int currentJob = 0;
        for(int i = 0; i < numJobs; i++)
        {
            // create slices to reduce amount of copy later on
            NativeSlice<byte> jobResult = new NativeSlice<byte>(result, start, intermediateOutputLength[i]);

            ConversionJob convJob = new ConversionJob() { pixelValues = values[i], result = jobResult, type = ImageType.BGRA, id = jobIDCounter };
            jobHandles.Add(convJob.Schedule());

            start += intermediateOutputLength[i];
            currentJob++;
        }
        currentJob = 0;
        // schedule jobs and wait for completion
        foreach (JobHandle jobHandle in jobHandles)
        {
            jobHandle.Complete();
        }
        
        // collect intermediate results and reconstruct whole image
        currentJob = 0;
        List<byte> output = new List<byte>();
        // adding image indicator and opening brackets
        output.AddRange(Encoding.UTF8.GetBytes("i["));

        output.AddRange(result);
        // adding closing brackets and separator
        output.AddRange(Encoding.UTF8.GetBytes("];"));

        result.Dispose();
        return output.ToArray();
    }

    /// <summary>
    /// Converts a Vector2 into python readable form
    /// </summary>
    /// <param name="point">the point to convert</param>
    /// <returns>string containing python readable form of Vector2</returns>
    public static byte[] ConvertVector2ToByteString(Vector2 point)
    {
        Debug.Log(point.ToString());
        StringBuilder sb = new StringBuilder();
        sb.Append("p[");
        sb.Append(point.x.ToString());
        sb.Append(",");
        sb.Append(point.y.ToString());
        sb.Append("];");
        string vectorString = sb.ToString();
        byte[] convertedPoint = Encoding.Default.GetBytes(vectorString);
        string vectorUFT8String = Encoding.UTF8.GetString(convertedPoint);

        return convertedPoint;
    }

}



/// <summary>
/// Converts an image row into a python numpy readable form:
/// [[r11,g11,b11],[r21,g21,b21],],[[r12,g12,b12],[r22,g22,b22],],
/// </summary>
struct ConversionJob : IJob
{

    /// <summary>
    /// holds input values of this pixel row
    /// </summary>
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<byte> pixelValues;

    /// <summary>
    /// defines the image type (RGBA or DEPTH)
    /// </summary>
    [ReadOnly]
    public ImageType type;

    /// <summary>
    /// jobId for debugging purposes
    /// </summary>
    [ReadOnly]
    public int id;

    /// <summary>
    /// python readable form of image row will be saved in here
    /// </summary>
    [NativeDisableContainerSafetyRestriction] // allows writing to the same nativearray using native slices at the same time
    public NativeSlice<byte> result;

    public void Execute()
    {
        int resultCounter = 0;
        var tmp = Encoding.UTF8.GetBytes("[");
        foreach (byte value in tmp)
        {
            result[resultCounter] = value;
            resultCounter++;
        }
        for (int i = 0; i < pixelValues.Length; i += 3)
        {
            if (type == ImageType.BGRA)
            {
                // encode 1 pixel in byte array
                string currentPixel = "[" + pixelValues[i].ToString() + "," + pixelValues[i + 1].ToString() + "," + pixelValues[i + 2].ToString() + "],";
                byte[] tmpValue = Encoding.UTF8.GetBytes(currentPixel);
                foreach (byte value in tmpValue)
                {
                    result[resultCounter] = value;
                    resultCounter++;
                }
            }

        }
        tmp = Encoding.UTF8.GetBytes("],");
        foreach (byte value in tmp)
        {
            result[resultCounter] = value;
            resultCounter++;
        }
    }
}

