using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GimProperty : MonoBehaviour
{
	internal string filePath;
	internal readonly List<string> filePaths = new List<string>();

	internal void AddFilePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		filePath = path;
		if (!filePaths.Contains(path))
		{
			filePaths.Add(path);
		}
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
