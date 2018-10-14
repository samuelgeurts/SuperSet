//using UnityEngine;
//using System.Collections;
//using UnityEngine.UI;
//using System.IO;
//
//public class CSVParsing : MonoBehaviour
//{
//	public TextAsset csvFile; // Reference of CSV file
////	public InputField rollNoInputField;// Reference of rollno input field
////	public InputField nameInputField; // Reference of name input filed
//	public Text contentArea; // Reference of contentArea where records are displayed
//
//	private char lineSeperater = '\n'; // It defines line seperate character
//	private char fieldSeperator = ','; // It defines field seperate chracter
//
//	private string lines;
//	private string lineDAta;
//	public string records;
//	//public float x;
//
//
//	void Start ()
//	{
//		readData ();
//	}
//	// Read data from CSV file
//	private void readData()
//	{
//
//		string[] records = csvFile.text.Split (lineSeperater);
//
//		string[] parts = records [1].Split (fieldSeperator);
//
//		lines = parts[1];
//
//
////		lines = records.Split("\n"[0]);
////		lineDAta = (lines[0].Trim()).Split(","[0]);
//
////	 	var lines : String[] = records.Split("\n"[0]);
////		var lineData : String[] = (lines[0].Trim()).Split(","[0]);
////		var x : float;
////		float.TryParse(lineData[0], x);
//
//
//
//
////		foreach (string record in records)
////		{
////			string[] fields = record.Split(fieldSeperator);
////			foreach(string field in fields)
////			{
////				contentArea.text += field + "\t";
////			}
////			contentArea.text += '\n';
////		}
//	}
//
//
//
//
////	var fileData : String  = System.IO.File.ReadAllText(path)
////		var lines : String[] = fileData.Split("\n"[0]);
////	var lineData : String[] = (lines[0].Trim()).Split(","[0]);
////	var x : float;
////	float.TryParse(lineData[0], x);
////
////
////
//
//
//
//
//
//
//
//
//
//	// Add data to CSV file
////	public void addData()
////	{
////		// Following line adds data to CSV file
////		File.AppendAllText(getPath() + "/Assets/StudentData.csv",lineSeperater + rollNoInputField.text + fieldSeperator +nameInputField.text);
////		// Following lines refresh the edotor and print data
////		rollNoInputField.text = "";
////		nameInputField.text = "";
////		contentArea.text = "";
////		#if UNITY_EDITOR
////		UnityEditor.AssetDatabase.Refresh ();
////		#endif
////		readData();
////	}
//
////	// Get path for given CSV file
////	private static string getPath(){
////		#if UNITY_EDITOR
////		return Application.dataPath;
////		#elif UNITY_ANDROID
////		return Application.persistentDataPath;// +fileName;
////		#elif UNITY_IPHONE
////		return GetiPhoneDocumentsPath();// +"/"+fileName;
////		#else
////		return Application.dataPath;// +"/"+ fileName;
////		#endif
////		}
////		// Get the path in iOS device
////		private static string GetiPhoneDocumentsPath()
////		{
////		string path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
////		path = path.Substring(0, path.LastIndexOf('/'));
////		return path + "/Documents";
////		}
//
//}
