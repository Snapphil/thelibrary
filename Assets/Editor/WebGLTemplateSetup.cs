using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates a WebXR-compatible WebGL template
/// </summary>
public class WebGLTemplateSetup
{
    [MenuItem("VR Assignment/Create WebXR Template")]
    public static void CreateWebXRTemplate()
    {
        Debug.Log("Creating WebXR Template...");
        
        // Create WebGLTemplates directory
        string templatesPath = Path.Combine(Application.dataPath, "WebGLTemplates");
        string webxrTemplatePath = Path.Combine(templatesPath, "WebXR");
        
        if (!Directory.Exists(templatesPath))
        {
            Directory.CreateDirectory(templatesPath);
        }
        
        if (!Directory.Exists(webxrTemplatePath))
        {
            Directory.CreateDirectory(webxrTemplatePath);
        }
        
        // Create index.html template
        string indexHtml = @"<!DOCTYPE html>
<html lang=""en-us"">
<head>
    <meta charset=""utf-8"">
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=no"">
    <title>{{{ PRODUCT_NAME }}}</title>
    <style>
        * { margin: 0; padding: 0; }
        html, body { width: 100%; height: 100%; overflow: hidden; }
        #unity-container { width: 100%; height: 100%; position: absolute; }
        #unity-canvas { width: 100%; height: 100%; background: #231F20; }
        #unity-loading-bar { position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%); }
        #unity-progress-bar-empty { width: 300px; height: 20px; background: #1a1a1a; border: 2px solid #444; border-radius: 10px; }
        #unity-progress-bar-full { width: 0%; height: 100%; background: linear-gradient(90deg, #4CAF50, #8BC34A); border-radius: 8px; transition: width 0.3s; }
        #unity-footer { display: none; }
        .webxr-button {
            position: absolute;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            padding: 15px 30px;
            font-size: 18px;
            font-weight: bold;
            color: white;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border: none;
            border-radius: 30px;
            cursor: pointer;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
            transition: all 0.3s ease;
            z-index: 1000;
        }
        .webxr-button:hover {
            transform: translateX(-50%) scale(1.05);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.6);
        }
        .webxr-button:disabled {
            background: #666;
            cursor: not-allowed;
            box-shadow: none;
        }
        #loading-text {
            color: white;
            font-family: Arial, sans-serif;
            text-align: center;
            margin-top: 10px;
        }
    </style>
</head>
<body>
    <div id=""unity-container"">
        <canvas id=""unity-canvas"" tabindex=""-1""></canvas>
        <div id=""unity-loading-bar"">
            <div id=""unity-progress-bar-empty"">
                <div id=""unity-progress-bar-full""></div>
            </div>
            <div id=""loading-text"">Loading VR Experience...</div>
        </div>
    </div>
    <button id=""vr-button"" class=""webxr-button"" style=""display: none;"">Enter VR</button>
    
    <script src=""{{{ LOADER_FILENAME }}}""></script>
    <script>
        var container = document.querySelector(""#unity-container"");
        var canvas = document.querySelector(""#unity-canvas"");
        var loadingBar = document.querySelector(""#unity-loading-bar"");
        var progressBarFull = document.querySelector(""#unity-progress-bar-full"");
        var loadingText = document.querySelector(""#loading-text"");
        var vrButton = document.querySelector(""#vr-button"");
        
        var buildUrl = ""Build"";
        var loaderUrl = buildUrl + ""/{{{ LOADER_FILENAME }}}"";
        var config = {
            dataUrl: buildUrl + ""/{{{ DATA_FILENAME }}}"",
            frameworkUrl: buildUrl + ""/{{{ FRAMEWORK_FILENAME }}}"",
            codeUrl: buildUrl + ""/{{{ CODE_FILENAME }}}"",
            streamingAssetsUrl: ""StreamingAssets"",
            companyName: ""{{{ COMPANY_NAME }}}"",
            productName: ""{{{ PRODUCT_NAME }}}"",
            productVersion: ""{{{ PRODUCT_VERSION }}}"",
        };
        
        if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
            container.className = ""unity-mobile"";
        }
        
        createUnityInstance(canvas, config, (progress) => {
            progressBarFull.style.width = 100 * progress + ""%"";
            loadingText.textContent = ""Loading... "" + Math.round(100 * progress) + ""%"";
        }).then((unityInstance) => {
            loadingBar.style.display = ""none"";
            
            // Check for WebXR support
            if (navigator.xr) {
                navigator.xr.isSessionSupported('immersive-vr').then((supported) => {
                    if (supported) {
                        vrButton.style.display = ""block"";
                        vrButton.onclick = () => {
                            unityInstance.Module.WebXR.toggleVR();
                        };
                    }
                });
            }
        }).catch((message) => {
            alert(message);
        });
    </script>
</body>
</html>";
        
        File.WriteAllText(Path.Combine(webxrTemplatePath, "index.html"), indexHtml);
        
        // Create thumbnail (simple placeholder)
        CreateThumbnail(webxrTemplatePath);
        
        AssetDatabase.Refresh();
        
        Debug.Log("WebXR Template created at: " + webxrTemplatePath);
        
        EditorUtility.DisplayDialog(
            "WebXR Template Created",
            "A WebXR-compatible template has been created!\n\n" +
            "To use it:\n" +
            "1. Go to Edit → Project Settings → Player\n" +
            "2. Select WebGL settings tab\n" +
            "3. Under 'Resolution and Presentation'\n" +
            "4. Set WebGL Template to 'WebXR'\n\n" +
            "This template includes an 'Enter VR' button for VR headsets.",
            "OK"
        );
    }
    
    private static void CreateThumbnail(string templatePath)
    {
        // Create a simple thumbnail texture
        Texture2D thumbnail = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];
        
        // Create a gradient background
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float t = (float)y / 128f;
                pixels[y * 128 + x] = Color.Lerp(new Color(0.4f, 0.5f, 0.9f), new Color(0.5f, 0.3f, 0.6f), t);
            }
        }
        
        // Add ""VR"" text area (simplified)
        for (int y = 40; y < 88; y++)
        {
            for (int x = 30; x < 98; x++)
            {
                if (y >= 50 && y <= 78 && x >= 40 && x <= 88)
                {
                    pixels[y * 128 + x] = Color.white;
                }
            }
        }
        
        thumbnail.SetPixels(pixels);
        thumbnail.Apply();
        
        byte[] pngData = thumbnail.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(templatePath, "thumbnail.png"), pngData);
        
        Object.DestroyImmediate(thumbnail);
    }
}

