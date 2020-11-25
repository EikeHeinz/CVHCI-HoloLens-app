using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.WebCam;

public struct CameraResolution
{
    public int width;
    public int height;

    public CameraResolution(int width, int height)
    {
        this.width = width;
        this.height = height;
    }
}

public class PhotoProvider : MonoBehaviour
{
    PhotoCapture photoCaptureObj = null;
    Vector3 currentEndPoint;

    private static readonly int customWidth = 640;
    private static readonly int customHeight = 360;

    private static CameraResolution currentCameraRes = new CameraResolution(customWidth, customHeight);

    public void Start()
    {
        Debug.Log("PP: Initializing PhotoCapture");
        foreach (Resolution res in PhotoCapture.SupportedResolutions)
        {
            Debug.Log(res);
        }
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        Debug.Log("initializing photo capture service");
        Debug.Log("PP: PhotoCapture initialized");
    }


    void OnPhotoCaptureCreated(PhotoCapture captureObj)
    {
        Debug.Log("PP: configuring camera");
        photoCaptureObj = captureObj;

        CameraParameters c = new CameraParameters
        {
            hologramOpacity = 0.0f,
            cameraResolutionWidth = currentCameraRes.width,
            cameraResolutionHeight = currentCameraRes.height,
            pixelFormat = CapturePixelFormat.BGRA32
        };
        Debug.Log("PP: Created camera parameters");
        captureObj.StartPhotoModeAsync(c, OnPhotoModeStarted);
        Debug.Log("PP: PhotoCapture initialized");
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObj.Dispose();
        photoCaptureObj = null;
        Debug.Log("PP: stopped photo mode");
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("PP: Camera is ready");
        }
        else
        {
            Debug.LogError("PP: Unable to start photo mode!");
        }
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("PP: Saved Photo to disk!");
            photoCaptureObj.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        else
        {
            Debug.Log("PP: Failed to save photo to disk");
        }
    }
    public void TakePhoto()
    {
        Debug.Log("PP: Capturing Image");
        photoCaptureObj.TakePhotoAsync(OnCapturedPhotoToMemoryCallback);

    }

    public void TakePhoto(Vector3 endPoint)
    {
        Debug.Log("PP: Capturing Image");
        currentEndPoint = endPoint;
        
        photoCaptureObj.TakePhotoAsync(OnCapturedPhotoToMemoryCallback);

    }

    void OnCapturedPhotoToMemoryCallback(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Debug.Log("PP: Captured Image");
            // hand raw photo data to network sender and send them via tcp to server
            // photoCaptureFrame.
            photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 camToWorld);
            photoCaptureFrame.TryGetProjectionMatrix(out Matrix4x4 camProjection);
            Debug.Log("PP: camToWorld: " + camToWorld);
            Matrix4x4 worldToCam = camToWorld.inverse;
            Vector3 pointCam = worldToCam.MultiplyPoint(currentEndPoint);
            Debug.Log("PP: Point in cam: " + pointCam);
            Vector3 projectedPoint = camProjection.MultiplyPoint(pointCam);

            // convert point from normalized device coordinates into image space
            Vector2 finalPoint = new Vector2
            {
                x = projectedPoint.x * currentCameraRes.width,
                y = projectedPoint.y * currentCameraRes.height
            };

            List<byte> imgBufferList = new List<byte>();
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imgBufferList);
            photoCaptureFrame.Dispose();
            
            Debug.Log("PixelSum: " + imgBufferList.Count / 4);

            StringBuilder sb = new StringBuilder();

            byte[] convImg = ImageConverter.ConvertImageToPythonParallel(imgBufferList.ToArray(), ImageType.BGRA, currentCameraRes);
            Debug.Log("Converted in parallel");
            int imageLength = imgBufferList.Count / 4;
            Debug.Log("imageLength:" + imageLength);
            Debug.Log("convImg Len: " + convImg.Length);
            imgBufferList.Clear();

            string tmpMsg = Encoding.UTF8.GetString(convImg);
            sb.Append(tmpMsg);

            sb.Append(Encoding.UTF8.GetString(ImageConverter.ConvertVector2ToByteString(finalPoint)));
            Debug.Log("Final length:" + sb.Length);

            Debug.Log("Sending data...");
            string response = AsynchronousClient.Run(sb.ToString());
            ObjectLoader objLoader = gameObject.GetComponent<ObjectLoader>();
            objLoader.DisplayObject(response);
        }
        else
        {
            Debug.Log("PP: Failed to capture image");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
