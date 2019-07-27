using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QDollarGestureRecognizer;
using PDollarGestureRecognizer;
using System.IO;
using System.Xml;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class writeGesture : MonoBehaviour {

    public static writeGesture _instance = null;
    //Points to create training sets
    List<PDollarGestureRecognizer.Point> points = new List<PDollarGestureRecognizer.Point>();
    Gesture[] trainingSet = null;
    
    private int strokeIndex = -1;

    public bool useEarlyAbandoning = true;
    public bool useLowerBounding = true;

    public float variationAllowance = 8f;

    public enum gestureTypes {N, T, X, A, H, C, Y, K, U, Line};
    public gestureTypes currentType = gestureTypes.N;

    public GameObject linePrefab;
    public List<LineRenderer> allLines;
    private int curLine = 0;

    public Text txt;
    private bool drawing = false;
    public GameObject canvas;

    private GraphicRaycaster raycaster;
    private EventSystem eventSys;
    private PointerEventData pointerEventData;

    //True while training set loads
    private bool loading = true;
    //Obejct to show training sets loading
    public GameObject loader;

    //Display heart and heartbeat with certain texts
    private bool displayed = false;
    private bool beat = false;
    public GameObject heart;

    // Use this for initialization
    void Start () {
        if(_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSys = canvas.GetComponent<EventSystem>();

        Invoke("startLoad", 1f);
    }
	
    //Load training sets and show load completed by deleting the object.
    void startLoad()
    {
        trainingSet = loadTrainingSet();
        loading = false;
        Destroy(loader);
    }

	// Update is called once per frame
    //Drawing gestures and calling functions if the gesture is correct from here
	void Update () {

        if (!loading)
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                pointerEventData = new PointerEventData(eventSys);
                pointerEventData.position = Input.mousePosition;
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointerEventData, results);

                drawing = true;
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.name == "Enter" || result.gameObject.name == "Clear")
                    {
                        drawing = false;
                    }
                }
                if (drawing)
                {
                    strokeIndex++;
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    GameObject obj = GameObject.Instantiate(linePrefab, mousePos, Quaternion.identity);
                    allLines.Add(obj.GetComponent<LineRenderer>());
                }
            }
            else if (Input.GetMouseButton(0) && drawing)
            {
                points.Add(new PDollarGestureRecognizer.Point(Input.mousePosition.x, Input.mousePosition.y, strokeIndex));

                Vector3 end = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                end = new Vector3(end.x, end.y, 0f);
                allLines[curLine].positionCount++;
                allLines[curLine].SetPosition(allLines[curLine].positionCount - 1, end);
            }
            else if (Input.GetMouseButtonUp(0) && drawing)
            {
                //drawLine();
                curLine++;
                drawing = false;
            }
            //Right click to save new training set data
            if (Input.GetMouseButtonDown(1))
            {
                ClickToSaveGesture();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                RecognizeGesture();
                points.Clear();
                for (int i = 0; i < allLines.Count; i++)
                {
                    Destroy(allLines[i].gameObject);
                }
                allLines.Clear();
                curLine = 0;
                strokeIndex = 0;
            }
#endif

#if !UNITY_EDITOR
            //FOR MOBILE
            if(Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pointerEventData = new PointerEventData(eventSys);
                pointerEventData.position = Input.GetTouch(0).position;
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointerEventData, results);

                drawing = true;
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.name == "Enter" || result.gameObject.name == "Clear")
                    {
                        drawing = false;
                    }
                }

                if(drawing)
                {
                    strokeIndex++;
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
                    GameObject obj = GameObject.Instantiate(linePrefab, mousePos, Quaternion.identity);
                    allLines.Add(obj.GetComponent<LineRenderer>());
                }
            }
            else if(Input.GetTouch(0).phase == TouchPhase.Moved && drawing)
            {
                points.Add(new PDollarGestureRecognizer.Point(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, strokeIndex));

                Vector3 end = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
                end = new Vector3(end.x, end.y, 0f);
                allLines[curLine].positionCount++;
                allLines[curLine].SetPosition(allLines[curLine].positionCount - 1, end);
            }
            else if(Input.GetTouch(0).phase == TouchPhase.Ended && drawing)
            {
                curLine++;
                drawing = false;
            }
#endif

            if(txt.text == "ANCY" && !displayed)
            {
                displayHeart();
            }
            else if(txt.text == "ANNAKUTTY" && !beat && displayed)
            {
                beat = true;
                heartBeat();
            }

        }
	}
    
    //Display heart
    private void displayHeart()
    {
        heart.SetActive(true);
        displayed = true;
    }

    //Make the heart beat
    private void heartBeat()
    {
        heart.GetComponent<Animator>().SetTrigger("beat");
    }

    //For debugging
    public void loadCheck()
    {
        TextAsset[] str = Resources.LoadAll<TextAsset>("Gestures/");
        if (str != null)
        {
            //Gesture gest = readGesture(str[0].text);
            foreach(TextAsset file in str)
            {
                //Debug.Log(file.name);
            }
        }
        else
        {
            Debug.Log(str);
        }
    }

    //Load trainging set data and store them
    private Gesture[] loadTrainingSet()
    {
        List<Gesture> gestures = new List<Gesture>();
        TextAsset[] gestureFiles = Resources.LoadAll<TextAsset>("Gestures/");
        foreach (TextAsset file in gestureFiles)
        {
            string str = file.text;
            try
            {
                gestures.Add(readGesture(str));
            }
            catch(Exception e)
            {
                Debug.LogException(e, this);
            }
        }
        return gestures.ToArray();
    }

    //Save new training set data to PersistantDataPath
    private void saveGesture(PDollarGestureRecognizer.Point[] points, string gestureName, string fileName)
    {
        if (!Directory.Exists(Application.persistentDataPath + "\\Gestures"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "\\Gestures");
        }
        if (!Directory.Exists(Application.persistentDataPath + "\\Gestures\\" + currentType.ToString()))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "\\Gestures\\" + currentType.ToString() + ".xml");
        }
        using (StreamWriter sw = new StreamWriter(fileName))
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
            sw.WriteLine("<Gesture Name = \"{0}\">", gestureName);
            int currentStroke = -1;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].StrokeID != currentStroke)
                {
                    if (i > 0)
                        sw.WriteLine("\t</Stroke>");
                    sw.WriteLine("\t<Stroke>");
                    currentStroke = points[i].StrokeID;
                }

                sw.WriteLine("\t\t<Point X = \"{0}\" Y = \"{1}\" T = \"0\" Pressure = \"0\" />",
                    points[i].X, points[i].Y
                );
            }
            sw.WriteLine("\t</Stroke>");
            sw.WriteLine("</Gesture>");
        }
    }

    //Read a gesture after input
    private Gesture readGesture(string fileName)
    {
        List<Point> points = new List<Point>();
        int currentStrokeIndex = -1;
        string gestureName = "";

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(fileName);
        XmlElement root = xmlDoc.DocumentElement;
        //Gesture name from root node
        gestureName = root.Attributes["Name"].Value;

        XmlNodeList nodeList = root.SelectNodes("Stroke");
        for(int i=0;i<nodeList.Count;i++)
        {
            XmlNodeList pointsList = nodeList[i].ChildNodes;
            for(int j=0;j<pointsList.Count;j++)
            {
                points.Add(new Point(float.Parse(pointsList[j].Attributes["X"].Value),
                    float.Parse(pointsList[j].Attributes["Y"].Value), 
                    currentStrokeIndex));
            }
            currentStrokeIndex++;
        }
        
        //Return gesture
        return new Gesture(points.ToArray(), gestureName);
    }

    //Recognize a gesture
    private void RecognizeGesture()
    {
        Gesture candidate = new Gesture(points.ToArray(), "tester");
        string gestureClass = "";
        QPointCloudRecognizer.UseEarlyAbandoning = useEarlyAbandoning;
        QPointCloudRecognizer.UseLowerBounding = useLowerBounding;
        gestureClass = QPointCloudRecognizer.Classify(candidate, trainingSet);
        Debug.Log("Recognized as: " + gestureClass);
        txt.text += gestureClass;
    }

    //Call recognize gesture on button click
    public void ClickToRecognize()
    {
        RecognizeGesture();
        points.Clear();
        for (int i = 0; i < allLines.Count; i++)
        {
            Destroy(allLines[i].gameObject);
        }
        allLines.Clear();
        curLine = 0;
        strokeIndex = 0;
    }

    //Clear saved text on click
    public void ClearText()
    {
        txt.text = "";
    }

    //Save new training set data on click
    public void ClickToSaveGesture()
    {
        saveGesture(points.ToArray(), currentType.ToString(), currentType.ToString() + System.DateTime.Now.ToFileTime() + ".txt");
        points.Clear();
        for (int i = 0; i < allLines.Count; i++)
        {
            Destroy(allLines[i].gameObject);
        }
        allLines.Clear();
        curLine = 0;
        strokeIndex = 0;
    }

}
