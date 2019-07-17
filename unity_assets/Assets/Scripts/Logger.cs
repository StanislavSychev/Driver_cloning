using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
//using UnityEditor;
//using UnityEditor;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Logger : NetworkBehaviour {

    public static int CRASHES = 0;
    public static float MAXVELOCITY = 0f;

    public static List<float> VELOCITY;
    public static List<float> LANEHYGEINE;
    public static List<float> CLOSESTPROX;

    public static Logger INSTANCE;

    public LoggingType LogTo = LoggingType.Cloud;

    public bool CollisionHappened = false;
    public Aspect ColliderAspect = null;
    public bool CollidedWithCar = false;
    public float CollisionCountdown = 0f;

    public bool LogCarPosition = true;
    public bool LogScenarioStart = true;
    public bool LogScenarioEnd = true;

    public bool LogCarVelocity = true;
    public bool LogCarCollisions = true;
    public bool LogCarProximity = true;


    public List<Logger> OtherLoggers = new List<Logger>();
    public HashSet<string> Scenarios = new HashSet<string>();

    private Aspect MyAspect;
    private CarSensor MySensor;
    private Signal MySignal;

    public enum LoggingType
    {
        Console, File, Cloud
    }

    public void Awake()
    {
    }

    public void Start(){
        
        //System.IO.File.WriteAllText(@"C:\Users\vanoi_000\Documents\unity1816\Social Driving AI\Output\LOGGERSTART"+System.DateTime.Now.ToString("dd-MMMM-yyyy-H-mm-ss")+".txt",gameObject.name);

        
        if (isLocalPlayer) { 
        INSTANCE = this;
        }

        MyAspect = GetComponent<Aspect>();
        MySensor = GetComponent<CarSensor>();
        MySignal = GetComponent<Signal>();


        CRASHES = 0;
        MAXVELOCITY = 0f;

	}
    /*
   **8888888*88888888888888888888888888888-------------------------------------------------------------------------------------------------------------888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888*-private void OnDrawGizmos()
    {
        Handles.color = Color.yellow;

        Vector3 pos1 = transform.position;
        Vector3 pos2 = ;

        Vector3[] v3 = new Vector3[] { pos1, pos2 };
        Handles.DrawAAPolyLine(10f, v3);


    }
    */

    [ClientRpc]
	public void RpcLogLevel(string filename)
    {
        System.IO.File.WriteAllText(@"C:\Users\vanoi_000\Documents\unity1816\Social Driving AI\Output\LOGGERRPC"+System.DateTime.Now.ToString("dd-MMMM-yyyy-H-mm-ss")+".txt","start");
        if (!isLocalPlayer) { return; }

        string message = "";

        Dictionary<string, int> scenarios = ScenarioOverview.SCENARIOCOUNT;

        foreach (string scName in scenarios.Keys)
        {
            message += scName + ": ";
            message += scenarios[scName] + "";
            message += "; ";
        }



        Debug.Log(message);


        EventData e = new EventData();

        //TODO: Work out what kind of data (if any) Cade wants here...

        Debug.Log("LOGGING LEVEL DATA!");
        e.Add("Level ID", filename);
		e.Add("Player ID", gameObject.name);
        e.Add("Horn pressed seconds", Horn.TIMEPRESSED + "");
        e.Add("Horn pressed instances", Horn.DISCRETETIMESPRESSED + "");
        
        e.Add("Times signalled", Signal.TIMESACTIVATED + "");
        e.Add("Crashes", Logger.CRASHES + "");
        e.Add("Average Velocity", Avg(Logger.VELOCITY) + "");
        e.Add("Maximum Velocity", Logger.MAXVELOCITY + "");
        e.Add("Average distance from lane centre", Avg(Logger.LANEHYGEINE) + "");
        e.Add("Average distance to nearest car", Avg(Logger.CLOSESTPROX) + "");

        Analytics.LogEvent(e);
        Debug.Log("LOGGED WHOLE PLAYER FILE");

    }


    public float Avg(List<float> list)
    {

        float total = 0f;

        foreach (float f in list)
        {
            total += f;
        }

        total /= list.Count;

        return total;

    }

    /*
    public void Draw(List<DataTrajectory> dd)
    {

        for (int i = 0; i < dd.Count; i++)
        {
            DataTrajectory d = dd[i];

            Handles.Label(d.Start + (Vector3.up), ":" + i + ":");



            Handles.DrawLine(d.Start, d.End);

            if (d.length > 1f) {
                Handles.ArrowCap(0, d.Start, Quaternion.LookRotation(d.End - d.Start, Vector3.up), 3f);
            }
        }

    }

    public void Draw(Aspect a, float dist)
    {
        if (a != null) {
            Handles.color = Color.green;


            Handles.DrawLine(MyAspect.sensor.position, a.sensor.position);
            Handles.ArrowCap(0, MyAspect.sensor.position, Quaternion.LookRotation(a.sensor.position - MyAspect.sensor.position, Vector3.up), 3f);

            Vector3 pos = Vector3.Lerp(MyAspect.sensor.position, a.sensor.position, 0.5f);

            Handles.Label(pos, "Dist to " + a.gameObject + ":" + dist);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && false) {

            float distAhead; float distBehind;

            List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(100f);
            Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);

            //  Draw(ahead);
            Draw(closestAhead, distAhead);


            List<DataTrajectory> behind = MySensor.ProjectStraightBehind(100f);
            Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);


            //    Draw(behind);
            Draw(closestBehind, distBehind);


            //          Junction   straightJ = s.nextJunction.GetStraightestFrom(s.currentJunction);





            //        Handles.DrawPolyLine(new Vector3[] {s.currentJunction.position,s.nextJunction.position,straightJ.position });


        }
    }
    */

    #region coroutines

    public IEnumerator LogProximity()
    {
        while (true)
        {
            if (LogCarProximity)
            {
                yield return new WaitForSeconds(0.5f);

                float distAhead, distBehind;

                List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(100f);
                List<DataTrajectory> behind = MySensor.ProjectStraightBehind(100f);

                Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);
                Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

                Aspect closest = null; float dist = 0f;



                if (closestAhead == null) { closest = closestBehind; dist = distBehind; }

                else if (closestBehind == null) { closest = closestAhead; dist = distAhead; }

                else
                {
                    if (distBehind < distAhead) { closest = closestBehind; dist = distBehind; }
                    else { closest = closestAhead; dist = distAhead; }
                }



                string carahead, carbehind, caroverall, proxahead, proxbehind, proxoverall;

                if (closestAhead == null)
                {
                    carahead = "NULL";
                    proxahead = "N/A";
                }
                else
                {
                    carahead = closestAhead.gameObject + "";
                    proxahead = distAhead + "";
                }

                if (closestBehind == null)
                {

                    carbehind = "NULL";
                    proxbehind = "N/A";
                }
                else
                {
                    carbehind = closestBehind.gameObject + "";
                    proxbehind = distBehind + "";
                }

                if (closest == null)
                {
                    caroverall = "NULL";
                    proxoverall = "N/A";
                }
                else
                {
                    caroverall = closest.gameObject + "";
                    proxoverall = dist + "";
                }

                Fact f = new Fact();

                f.Add("CAR", gameObject + "");
                f.Add("CARAHEAD", carahead);
                f.Add("PROXAHEAD", proxahead);
                f.Add("CARBEHIND", carbehind);
                f.Add("PROXBEHIND", proxbehind);
                f.Add("CARCLOSEST", caroverall);
                f.Add("PROXCLOSEST", proxoverall);

                f.AddTimeStamp();
                Log(f);

                if (dist > 0f && dist<9999f)
                {
                    CLOSESTPROX.Add(dist);
                }

            }

            yield return new WaitForSeconds(0.5f);

        }
    }

   

    public IEnumerator LogVelocity()
    {
        while (true)
        {

            if (MySensor.speed > MAXVELOCITY)
            {
                MAXVELOCITY = MySensor.speed;
            }

            if (LogCarVelocity) {
                Fact f = new Fact();
                f.Add("CAR", gameObject + "");
                f.Add("VEL", MySensor.speed.ToString("n2"));

                f.AddTimeStamp();
                Log(f);
            }

            VELOCITY.Add(MySensor.speed);
            LANEHYGEINE.Add(MySensor.laneDrift);

            yield return new WaitForSeconds(1f);

        }
    }

    public IEnumerator LogPosition()
    {
        while (true)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(transform.position + "," + transform.rotation + ";");
            foreach (Logger l in OtherLoggers)
            {
                sb.Append(l.transform.position + "," + l.transform.rotation + ";");
            }
            ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
            using (RequestSocket client = new RequestSocket())
            {

                string stt = sb.ToString();
                client.Connect("tcp://localhost:5555");
                client.SendFrame("stt" + stt);
                string message = null;
                bool gotMessage = false;
                while (!gotMessage)
                {
                    gotMessage = client.TryReceiveFrameString(out message);
                }

                //if (gotMessage) System.IO.File.WriteAllText(@"C:\Users\vanoi_000\Documents\unity1816\Social Driving AI\Output\LOGGER"+Time.time.ToString()+".txt", message);
            }
            NetMQConfig.Cleanup();
            //System.IO.File.WriteAllText(@"C:\Users\vanoi_000\Documents\unity1816\Social Driving AI\Output\LOGGER"+Time.time.ToString()+".txt", sb.ToString());
            if (LogCarPosition)
            {
                Fact f = new Fact();
                f.Add("CAR", gameObject + "");
                f.Add("POS", transform.position + "");
                f.Add("ROT", transform.rotation + "");
                f.Add("Hygeine", MySensor.laneDrift + "");
                f.Add("Signal", MySignal.State + "");
                f.AddTimeStamp();
                Log(f);

                foreach (Logger l in OtherLoggers)
                {
                    f = new Fact();
                    f.Add("CAR", l.gameObject + "");
                    f.Add("POS", l.transform.position + "");
                    f.Add("ROT", l.transform.rotation + "");
                    f.Add("Hygeine", l.MySensor.laneDrift + "");
                    f.Add("Signal", l.MySignal.State + "");
                    f.AddTimeStamp();
                    Log(f);
                }


            }
            yield return new WaitForSeconds(0.5f);

        }
    }

    #endregion



    // Use this for initialization
    void Setup() {

        //   if (!isLocalPlayer) { return; }

        if (isLocalPlayer)
        {
            SceneSelector.ID = this.gameObject.name;
        }
        VELOCITY = new List<float>();
        LANEHYGEINE = new List<float>();
        CLOSESTPROX = new List<float>();

        CRASHES = 0;
      //  Id = SceneSelector.ID;

        MyAspect = GetComponent<Aspect>();
        MySensor = GetComponent<CarSensor>();
        MySignal = GetComponent<Signal>();

        StartCoroutine(LogPosition());
        StartCoroutine(LogVelocity());
        StartCoroutine(LogProximity());

    }


    public void LogCollision(Collider c)
    {

        if (!isLocalPlayer) { return; }

		CollisionCountdown = 3f;
        CollisionHappened = true; ColliderAspect = c.GetComponentInParent<Aspect>();
        if (ColliderAspect != null) { CollidedWithCar = true; } else { CollidedWithCar = false; }

        Fact f = new Fact();

        Aspect a = c.gameObject.GetComponentInParent<Aspect>();
 
		if (a != null)
        {


            Debug.Log("MYASPECT:" + MyAspect);
            Debug.Log("MYASPECT SENSOR"+MyAspect.sensor);
            Debug.Log("MYASPECT SENSOR SPEED" + MyAspect.sensor.speed);

            f.Add("MYCAR", gameObject + "");
            f.Add("THEIRCAR", a.gameObject + "");
            f.Add("MYSPEED", MyAspect.sensor.speed + "");
            f.Add("THEIRSPEED", a.sensor.speed + "");
            f.Add("MYPOS", transform.position + "");
            f.Add("MYROT", transform.rotation + "");
            f.Add("THEIRPOS", a.transform.position + "");
            f.Add("THEIRROT", a.transform.rotation + "");

            f.AddTimeStamp();

            Log(f);
            CRASHES += 1;

        
		}
        else
        {
            f.Add("MYCAR", gameObject + "");
            f.Add("COLLIDED WITH SCENERY", c.gameObject + "");
            f.Add("MYPOS", transform.position + "");
            f.Add("MYROT", transform.rotation + "");

        }






    }

    bool Ready = false;

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {


        if (MySensor != null)
        {

            int i = 0;

            List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(200f);
            foreach (DataTrajectory d in ahead)
            {
                Handles.Label(d.Start+Vector3.up, i + "");
                i++;
                Debug.DrawLine(d.Start, d.Start + Vector3.up, Color.red);
                Debug.DrawLine(d.End, d.End + Vector3.up, Color.red);
                Debug.DrawLine(d.Start + (Vector3.up / 2f), d.End + (Vector3.up / 2f), Color.red);
            }

            float distAhead;
            Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);


        }
    }

#endif

    // Update is called once per frame
    void Update() {



        if (!isLocalPlayer) {

            if (INSTANCE.OtherLoggers.Contains(this) == false)
            {
                INSTANCE.OtherLoggers.Add(this);

            }

            return;
        }


       


        if (CollisionCountdown >= 0f)
        {
            CollisionCountdown -= Time.deltaTime;

            if (CollisionCountdown < 0f)
            {

                CollisionHappened = false;

            }

        }

        if (!Ready)
        {

            if (GetComponent<NetworkCarController>().PathSetup)
            {

                Setup();

                Ready = true;
            }

            return;

        }

        if (CollisionHappened) { return; }

        CheckCooperationScenarios();

        CheckTrustScenarios();

        CheckAttentionScenarios();

        CheckCommunicationScenarios();

        CheckContagionScenarios();


    }



    private Lane _oldLane;

    private Lane oldLane
    {
        get
        {
            if (_oldLane == null)
            {
                _oldLane = sensor.firstLane;
            }
            
            return _oldLane;

        }
    }

    private List<Aspect> Ahead = new List<Aspect>();
    private List<Aspect> Behind = new List<Aspect>();

    float signalLength = 0f;


    public bool InvolvedInScenario(string s)
    {
        return Scenarios.Contains(s);
    }

   private void RecordScenario(string scenario)
    {

        Scenarios.Add(scenario);


    }

    public bool IsInDenseTraffic()
    {

        float distAhead; float distBehind;

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(60f);
        Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);

        if(closestAhead == null) { return false; }


        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(60f);

        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        if(closestBehind == null) { return false; }


        return true;

    }

    public bool IsInDenseTraffic(out float distAhead, out float distBehind, out Aspect closestAhead, out Aspect closestBehind)
    {

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(100f);
        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(100f);

        closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);
        closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        if (closestAhead == null || closestBehind == null) { return false; }

        return true;

    }


    public CarSensor sensor
    {
        get
        {
            return MyAspect.sensor;
        }
    }




    #region Trust Scenarios


    public void CheckTrustScenarios()
    {
        CheckScenario_Trust1();
        CheckScenario_Trust2();
        CheckScenario_Trust3();
        CheckScenario_Trust4();
        CheckAndRecordScenario_Trust5();
    }

    public IEnumerator Trust1()
    {
        RecordScenario("Trust 1");

        /*
         * End trigger: Participant turns onto the road
         * Measures: How long does the participant take to pull out
         * Measures: How far does the participant leave between itself and a car behind it?
         * 
         */

        float startTime = Time.time;
        bool notFullyTurned = true;
        DataTrajectory d = MyAspect.sensor.fullTrajectories[0];
        IRoadSpace startLane = d.RoadSpace;
        while (notFullyTurned)
        {


            d = MyAspect.sensor.fullTrajectories[0];

            if (d.RoadSpace != startLane)
            {
                if (d.RoadSpace.spaceType == RoadSpaceType.Lane)
                {
                    notFullyTurned = false;
                    break;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }


        float timeTaken = Time.time - startTime;
        float distBehind;

        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(100f);
        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        Fact fact = new Fact();



        fact.Add("Scenario Completed", "Trust_1");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Time taken", timeTaken + "");
        fact.Add("Distance to car behind", distBehind + "");
        fact.Add("Car behind", closestBehind + "");
        fact.AddTimeStamp();

        Scenarios.Remove("Trust 1");

        Log(fact);

    }

    public IEnumerator Trust2()
    {
        RecordScenario("Trust 2");

        /*
         * End trigger is: There is no longer a vehicle between one car's length behind and one car's length in front of the participants
         * Measures: How close does the participant get to the other car (Continuous)
         * Measures: Lane accuracy score (metres from centre of lane)
         * Context to record:
         * Identity of any passing vehicle
         * Is there a vehicle within 100m in front in the same lane?
         * Is there a vehicle within 100m behind in the same lane?
         */

        float startTime = Time.time;

        float maxDrift = 0f;

        int fromEdge = sensor.firstLane.lanesFromEdge;

        float distAhead, distBehind;
        Aspect closestAhead, closestBehind;

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(60f);
        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(60f);

        closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);
        closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);


        while (true)
        {


            if (fromEdge != sensor.firstLane.lanesFromEdge)
            {
                fromEdge = sensor.firstLane.lanesFromEdge;
                maxDrift = 0f;
            }

            if (sensor.laneDrift > maxDrift)
            {
                maxDrift = sensor.laneDrift;
            }

            float tTaken = Time.time - startTime;


            if (!IsInDenseTraffic() && tTaken > 0.5f)
            {
                break;
            }

            ahead = MySensor.ProjectStraightAhead(60f);
            behind = MySensor.ProjectStraightBehind(60f);

            closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);
            closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

            yield return new WaitForSeconds(0.2f);
        }


        float timeTaken = Time.time - startTime;


        Fact fact = new Fact();


        fact.Add("Scenario Completed", "Trust_2");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Time taken", timeTaken + "");
        fact.Add("Distance to car behind", distBehind + "");
        fact.Add("Car behind", closestBehind + "");
        fact.Add("Distance to car ahead", distAhead + "");
        fact.Add("Car ahead", closestAhead + "");
        fact.Add("Lane hygeine", maxDrift + "");
        fact.AddTimeStamp();

        Scenarios.Remove("Trust 2");

        Log(fact);

    }

    public IEnumerator Trust3()
    {
        RecordScenario("Trust 3");

        /*
         * End trigger is: They enter the other lane (or begin the turn) DONE
         * Measures: 
         * Discrete - do they signal? DONE
         * Continuous - how far before the turn do they get into the current lane? DONE
         * Continuous - how long after signalling do they get into the correct lane? DONE
         * Continuous - how close do they get to the car behind it in the adjacent lane? DONE
         * Context - Identity of closest car behind in adjacent lane. DONE
         */

        float startTime = Time.time;

        int fromCentre = sensor.firstLane.lanesFromCentre;

        float distBehind = 0f;
        float distanceToTurn = 0f;
        Aspect closestBehind = null;

        bool signalled = false;

        float startedSignalling = 0f;

        while (true)
        {

            distanceToTurn = sensor.distanceToTurn;


            // If you've changed lanes, end the scenario

            if (sensor.firstLane.lanesFromCentre != fromCentre)
            {
                break;
            }

            // If you've begun the turn, end the scenario

            if (sensor.currentlyTurning)
            {
                distanceToTurn = 0f;
                break;
            }

            // If you're signalling, flag this.

            if (signalled == false)
            {
                if (MyAspect.signalling == true)
                {
                    startedSignalling = Time.time;
                    signalled = true;
                }
            }


            List<DataTrajectory> behind = CarSensor.ProjectStraightBehind(200f, sensor.firstLane.nextLaneOut, sensor.myAspect);
            closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

            yield return new WaitForSeconds(0.1f);
        }


        float timeTaken = Time.time - startTime;

        float signalLength = Time.time - startedSignalling;

        if (signalled == false) { signalLength = 0f; }

        Fact fact = new Fact();


        fact.Add("Scenario Completed", "Trust_3");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Time taken", timeTaken + "");
        fact.Add("Signalled", signalled + "");
        fact.Add("Signal Length", signalLength + "");
        fact.Add("Distance to turn", distanceToTurn + "");

        fact.Add("Distance to car in adjacent lane", distBehind + "");
        fact.Add("Car in adjacent lane", closestBehind + "");
        fact.AddTimeStamp();

        Scenarios.Remove("Trust 3");

        Log(fact);

    }

    public IEnumerator Trust4()
    {
        RecordScenario("Trust 4");

        /*
         * End trigger is: They enter the other lane (or begin the turn) DONE
         * Measures: 
         * Discrete - do they signal? DONE
         * Continuous - how far before the turn do they get into the current lane? DONE
         * Continuous - how long after signalling do they get into the correct lane? DONE
         * Continuous - how close do they get to the car behind it in the adjacent lane? DONE
         * Context - Identity of closest car behind in adjacent lane. DONE
         */



        float startTime = Time.time;

        int fromCentre = sensor.firstLane.lanesFromCentre;

        float distBehind = 0f;

        Aspect closestBehind = null;

        bool signalled = false;

        float startedSignalling = 0f;

        float distNarrow = 0f;

        while (true)
        {

            // If you've changed lanes, end the scenario

            if (sensor.firstLane.lanesFromCentre != fromCentre)
            {
                break;
            }


            // If you're signalling, flag this.

            if (signalled == false)
            {
                if (MyAspect.signalling == true)
                {
                    startedSignalling = Time.time;
                    signalled = true;
                }
            }

            distNarrow = sensor.distanceToNarrowing;

            List<DataTrajectory> behind = CarSensor.ProjectStraightBehind(200f, sensor.firstLane.nextLaneIn, sensor.myAspect);
            closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

            yield return new WaitForSeconds(0.1f);
        }


        float timeTaken = Time.time - startTime;

        float signalLength = Time.time - startedSignalling;

        if (signalled == false) { signalLength = 0f; }


        Fact fact = new Fact();


        fact.Add("Scenario Completed", "Trust_4");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Time taken", timeTaken + "");
        fact.Add("Signalled", signalled + "");
        fact.Add("Signal Length", signalLength + "");
        fact.Add("Distance to narrowing", distNarrow + "");

        fact.Add("Distance to car in adjacent lane", distBehind + "");
        fact.Add("Car in adjacent lane", closestBehind + "");
        fact.AddTimeStamp();

        Scenarios.Remove("Trust 4");

        Log(fact);

    }
    
    public void CheckScenario_Trust1()
    {
        /*
         * Scenario Trust 1 is...
         * Description is: 
         * The participant must pull out into dense traffic
         * Start trigger is: 
         * Participant is turning onto a major road
         */

        CarSensor s = MySensor;


        if (s.turnAtNextJunction)
        {
			if (s.nextTurn.MajorEnd == true)
            {
                if (InvolvedInScenario("Trust 1") == false)
                {

                    StartCoroutine(Trust1());
                }
            }
        }

    }

    public void CheckScenario_Trust2()
    {
        /*
         * Scenario Trust 2 is...
         * Description is: The participant is in dense traffic
         * Start trigger is: There is a vehicle between one car's length behind and one car's length in front of the participant
         * End trigger is: There is no longer a vehicle between two car's length behind and two car's length in front of the participants
         * Measures: How close does the participant get to the other car (Continuous)
         * Measures: Lane accuracy score (metres from centre of lane)
         * Context to record:
         * Identity of any passing vehicle
         * Is there a vehicle within 60m in front in the same lane?
         * Is there a vehicle within 60m behind in the same lane?
         */


        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(40f);
        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(40f);

        float distAhead; float distBehind;

        Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);
        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        if (closestAhead == null || closestBehind == null)
        {
            return;
        }


        if (InvolvedInScenario("Trust 2") == false)
        {

            StartCoroutine(Trust2());
        }


    }

    public void CheckScenario_Trust3()
    {
        /*
         * Scenario Trust 3 is...
         * Description is: The participant needs to get over to the adjacent lane in the midst of dense traffic in order to make an upcoming turn.
         * Start trigger is:
         * A
         * The participant is on a road with 2 or more lanes going in their direction
         * B
         * They have a turn approaching (within 200m) that requires them to get into the other lane
         * C
         * There is a vehicle in that adjacent lane that is between 0 and 200m behind the participant
         * End trigger is: They enter the other lane or make the turn
         * Discrete: Does the participant signal at all?
         * How far before the turn does the participant get into the other lane?
         * How long after signalling does the participant get into the other lane?
         * How close to the car behind it in the adjacent lane does the participant get?
         * Context to record:
         * Identity of vehicle behind in adjacent lane
         */



        // Check that the road has at least 2 lanes of traffic going their way

        if (sensor.currentlyTurning)
        {
            return;
        }

        if (sensor.firstLane.ParentRoad.NumberOfLanes < 4)
        {
            return;
        }

        // Check that it's under 200m to a turn

        if (sensor.distanceToTurn > 200f)
        {

            return;

        }

        // Check that the turn is a left one, and that one or more lanes need to be 'got across' to make it

        if (sensor.turnDirection == Direction.Left)
        {
            if (sensor.firstLane.lanesFromEdge == 0)
            {

                return;

            }
        }
        else
        {

            return;
        }
    


        // Check that there is a car between 0 and 200m behind the car in the adjacent lane.

        float distBehind;

        List<DataTrajectory> behind = CarSensor.ProjectStraightBehind(200f, sensor.firstLane.nextLaneOut, sensor.myAspect);

        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);


        if (closestBehind == null)
        {
            return;
        }

        if (InvolvedInScenario("Trust 3") == false)
        {

            StartCoroutine(Trust3());
        }


    }

    public void CheckScenario_Trust4()
    {
        /*
         */



        // Check that the road has at least 2 lanes of traffic going their way


        if (sensor.firstLane.ParentRoad.NumberOfLanes < 4)
        {
            return;
        }



        float ticker = 200f; // counts down from 200
        bool narrows = false;

        foreach (DataTrajectory t in sensor.fullTrajectories)
        {

            if (t.IsTurn())
            {
                return;
            }

            if (t.RoadSpace.spaceType == RoadSpaceType.Junction)
            {

                Lane startLane = t.previousLane;
                Lane endLane = t.nextLane;
                int laneDiff = startLane.ParentRoad.NumberOfLanes - endLane.ParentRoad.NumberOfLanes;

                if (laneDiff > 0)
                {
                    laneDiff /= 2;
                    if (startLane.lanesFromEdge < laneDiff)
                    {
                        narrows = true; break;
                    }
                }
            }


            ticker -= t.length;

            if (ticker < 0f)
            {
                return;
            }

        }

        if (!narrows)
        {
            return;
        }


        // Check that there is a car between 0 and 200m behind the car in the adjacent lane.

        float distBehind;

        List<DataTrajectory> behind = CarSensor.ProjectStraightBehind(200f, sensor.firstLane.nextLaneIn, sensor.myAspect);

        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);


        if (closestBehind == null)
        {
            return;
        }

        if (InvolvedInScenario("Trust 4") == false)
        {

            StartCoroutine(Trust4());
        }


    }

    public void CheckAndRecordScenario_Trust5()
    {

        if (MyAspect.signalling)
        {
            signalLength += Time.deltaTime;
        }
        else
        {
            signalLength = 0f;
        }

        if (oldLane.lanesFromCentre == sensor.firstLane.lanesFromCentre)
        {
            _oldLane = sensor.firstLane;
            return;
        }

        if (sensor.currentlyTurning)
        {
            return;
        }

        _oldLane = sensor.firstLane;




        // Check that there is a car between 0 and 200m behind the car in the new lane.

        float distBehind;

        List<DataTrajectory> behind = sensor.ProjectStraightBehind(200f);

        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);


        if (closestBehind == null)
        {
            return;
        }


        /*
         * Record data here
         */

        Fact fact = new Fact();


        fact.Add("Scenario Completed", "Trust_5");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Distance to car behind", distBehind + "");
        fact.Add("Car behind", closestBehind + "");
        fact.Add("Signalled", MyAspect.signalling + "");
        fact.Add("Signal Length", signalLength + "");
        fact.AddTimeStamp();

        Log(fact);



    }

    #endregion

    #region Contagion Scenarios

    public void CheckContagionScenarios()
    {

        List<Aspect> behind = new List<Aspect>();
        List<Aspect> ahead = new List<Aspect>();

        Vector3 positionFront = transform.position + (transform.forward * 0.05f); // this is a position around 5cm ahead of your centre

        foreach (Aspect a in RoadMaster.INSTANCE.Cars)
        {

            if (a.sensor.firstLane.ParentRoad != sensor.firstLane.ParentRoad)
            {
                continue;
            }

            Lane theirLane = a.sensor.firstLane;
            Lane myLane = sensor.firstLane;

            bool myLaneLow = myLane.LaneNumber < myLane.numberOfLanes / 2;
            bool theirLaneLow = theirLane.LaneNumber < theirLane.numberOfLanes / 2;

            if (myLaneLow != theirLaneLow)
            {
                continue;
            }

            float distFront = Vector3.Distance(a.sensor.position, positionFront);
            float dist = Vector3.Distance(a.sensor.position, sensor.position);

            if (distFront < dist)
            {
                ahead.Add(a);
            }
            else { behind.Add(a); }



        }

        foreach (Aspect a in ahead)
        {
            if (Behind.Contains(a))
            {

                if (InvolvedInScenario("Contagion 1") == false)
                {

                    StartCoroutine(Contagion1(a));
                }

            }
        }


        foreach (Aspect a in behind)
        {
            if (Ahead.Contains(a))
            {
                if (InvolvedInScenario("Contagion 2") == false)
                {
                    StartCoroutine(Contagion2(a));
                }
            }
        }


        Ahead = ahead;
        Behind = behind;


    }

    public IEnumerator Contagion1(Aspect otherCar)
    {

        Debug.Log("Starting contagion 1");

        RecordScenario("Contagion 1");

        /*
         * End trigger: 60 secs after start
         * Measures: Maximum and mean acceleration rate
         * Context: Is there a vehicle in front of the participant (less than 200m)
         * Context: Identity of passing vehicle
         */

        float speedAtStart = sensor.speed;

        float startTime = Time.time;

        float ticker = 10f;

        float maxAcc = 0f;

        float oldSpeed = speedAtStart;

        while (ticker > 0f)
        {
            ticker -= Time.deltaTime;

            float newSpeed = sensor.speed;
            float acc = (newSpeed - oldSpeed) / 0.1f;

            if (acc > maxAcc)
            {
                maxAcc = acc;
            }

            oldSpeed = newSpeed;


            yield return new WaitForSeconds(0.1f);
        }


        float acceleration = (sensor.speed - speedAtStart) / 10f;

        float distAhead;

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(200f);
        Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);

        Fact fact = new Fact();
        Debug.Log("Ending contagion 1");


        fact.Add("Scenario Completed", "Contagion_1");
        fact.Add("Scenario POV", gameObject+"");

        if (otherCar != null) { 
        fact.Add("Other car", otherCar.name + "");
        }
        else
        {
            fact.Add("Other car", "nothing");
        }

        fact.Add("Car ahead", closestAhead + "");
        fact.Add("Distance to car ahead", distAhead + "");

        fact.Add("Mean acceleration", acceleration + "");
        fact.Add("Maximum acceleration", maxAcc + "");


        fact.AddTimeStamp();

        Scenarios.Remove("Contagion 1");

        Log(fact);

    }

    public IEnumerator Contagion2(Aspect otherCar)
    {
        RecordScenario("Contagion 2");
        Debug.Log("Starting contagion 2");

        /*
         * End trigger: 60 secs after start
         * Measures: Maximum and mean acceleration rate
         * Context: Is there a vehicle behind the participant (less than 200m)
         * Context: Identity of passing vehicle
         */

        float speedAtStart = sensor.speed;

        float startTime = Time.time;

        float ticker = 10f;

        float maxAcc = 0f;

        float oldSpeed = speedAtStart;

        while (ticker > 0f)
        {
            ticker -= Time.deltaTime;

            float newSpeed = sensor.speed;
            float acc = (newSpeed - oldSpeed) / 0.1f;

            if (acc > maxAcc)
            {
                maxAcc = acc;
            }

            oldSpeed = newSpeed;


            yield return new WaitForSeconds(0.1f);
        }


        float acceleration = (sensor.speed - speedAtStart) / 10f;

        float distBehind;

        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(200f);
        Aspect closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        Fact fact = new Fact();
        Debug.Log("Ending contagion 2");

        fact.Add("Scenario Completed", "Contagion_2");
        fact.Add("Scenario POV", gameObject+"");
        if (otherCar != null) { 
        fact.Add("Other car", otherCar.name + "");
        }
        else
        {
            fact.Add("Other car", "nothing");
        }

        fact.Add("Car behind", closestBehind + "");
        fact.Add("Distance to car behind", distBehind + "");

        fact.Add("Mean acceleration", acceleration + "");
        fact.Add("Maximum acceleration", maxAcc + "");


        fact.AddTimeStamp();

        Scenarios.Remove("Contagion 2");

        Log(fact);

    }

    #endregion

    #region Cooperation Scenarios

    public void CheckCooperationScenarios()
    {

        CheckCooperationScenario1();
        CheckCooperationScenario2();
        CheckCooperationScenario3();


    }

    public IEnumerator Cooperation1(Aspect unluckyCar)
    {

        /*
         * END: Past the intersection
         * MEASURES: Discrete - does the participant let the vehicle in?
         * MEASURES: Continuous: Driver acceleration rate
         * CONTEXT: Identity of the other vehicle
         */

        //record that the scenario is in progress
        RecordScenario("Cooperation 1");

        bool letIn = false;

        int samples = 0;
        float accelerationRate = 0f;

        while (true)
        {

            accelerationRate += sensor.speed;
            samples += 1;
       
            

            // If collision happened, break out

            if (CollisionHappened) { break; }

            // If they have collided with something, break

            if (unluckyCar.RecentlyReset) { break; }

            // if you're close to narrowing, break out

            if (sensor.distanceToNarrowing < 0.5f) { break; }

            List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(sensor.distanceToNarrowing+10f);
            List<Aspect> carsAhead = DataTrajectory.AllCars(MyAspect, ahead);

            if (carsAhead.Contains(unluckyCar)) { letIn = true; }



            yield return null;

        }

        accelerationRate /= (float)samples;

        Aspect otherCollider;

        if (!CollisionHappened) { otherCollider = null; }
        else { otherCollider = ColliderAspect; }

        Fact fact = new Fact();

        fact.Add("Scenario Completed", "Cooperation_1");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Ended with collision", CollisionHappened + "");
        if (otherCollider != null)
        {
            fact.Add("Other collider", otherCollider + "");
        }
        else
        {
            fact.Add("Other collider", "nothing");
        }
        fact.Add("Other car", unluckyCar + "");
        fact.Add("Other car let in", letIn + "");
        fact.Add("Acceleration rate",accelerationRate + "");
        fact.AddTimeStamp();

        //record that the scenario is over
        Scenarios.Remove("Cooperation 1");

        Log(fact);


    }

    public IEnumerator Cooperation2(Aspect otherCar, Junction fourway)
    {
        /*
         * END: Participant gets through four-way stop
         * MEASURES: Discrete: Who goes first?
         * CONTEXT: Who should go first?
         * CONTEXT: How many vehicles are within 20m?
         * CONTEXT: Identity of other vehicles
         */

        //record that the scenario is in progress

        RecordScenario("Cooperation 2");

        int numberOfCarsAtFourway = fourway.CarsApproachingWithin(20f).Count;

        Aspect whoWentFirst = null;
        Aspect whoShouldGoFirst;

        if (Vector3.Distance(transform.position, fourway.position) < Vector3.Distance(otherCar.transform.position,fourway.position))
        {
            whoShouldGoFirst = MyAspect;
        }
        else
        {
            whoShouldGoFirst = otherCar;
        }



        while (true)
        {


            if (!sensor.currentTrajectory.isJunction)
            {
                if (!fourway.CarsApproachingWithin(20f).Contains(MyAspect))
                {
                    whoWentFirst = MyAspect; break;
                }
            }


            if (!otherCar.sensor.currentTrajectory.isJunction)
            {
                if (!fourway.CarsApproachingWithin(20f).Contains(otherCar))
                {
                    whoWentFirst = otherCar; break;
                }
            }



            // If collision happened, break out

            if (CollisionHappened) { break; }

            // If they have collided with something, break

            if (otherCar.RecentlyReset) { 
			
				Scenarios.Remove("Cooperation 2");
				yield break;
			
			}





            yield return null;

        }


        Aspect otherCollider;

        if (!CollisionHappened) { otherCollider = null; }
        else { otherCollider = ColliderAspect; }

        Fact fact = new Fact();

        fact.Add("Scenario Completed", "Cooperation_2");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Number of cars at fourway", numberOfCarsAtFourway + "");

        if (whoShouldGoFirst != null)
        {
            fact.Add("Who should go first", whoShouldGoFirst + "");
        }
        else
        {
            fact.Add("Who should go first", "nothing");
        }

        fact.Add("Who should go first", whoShouldGoFirst + "");

        if (whoWentFirst != null)
        {
            fact.Add("Who went first", whoWentFirst.gameObject.name + "");
        }
        else
        {
            fact.Add("Who went first", "nothing");
        }


        fact.Add("Ended with collision", CollisionHappened + "");

        if(otherCollider != null) {
            fact.Add("Other collider", otherCollider.gameObject.name + "");
        }
        else
        {
            fact.Add("Other collider", "nothing");
        }



        fact.Add("Other car", otherCar.gameObject.name + "");
        fact.AddTimeStamp();

        //record that the scenario is over
        Scenarios.Remove("Cooperation 2");

        Log(fact);


    }

    public IEnumerator Cooperation3(Aspect closest,bool inNotOut)
    {

        /*
         * END: The participant passes the other car OR the other car enters the participant's lane
         * MEASURES: Discrete: Does the participant allow the other to enter in front of them?
         * MEASURES: Continuous: How long between signal and entering the lane?
         * MEASURES: Continuous: Minimum deceleration?
         * CONTEXT: Is there a car behind (<100m) the participant?
         * CONTEXT: Identity of the other vehicle?
         */

        //record that the scenario is in progress
        RecordScenario("Cooperation 3");

        float timeTaken = 0f;

        bool letIn = false;

        float minimumDeceleration = sensor.acceleration;

        while (true)
        {

            timeTaken += Time.deltaTime;


            if (sensor.acceleration < minimumDeceleration) { minimumDeceleration = sensor.acceleration; }


            // If collision happened, break out

            if (CollisionHappened) { break; }

            // if you're close to narrowing, break out


			List<DataTrajectory> ahead = MySensor.ProjectStraightAhead (100f);

				//Mathf.Max(sensor.distanceToNarrowing + 10f,100f);
            List<Aspect> carsAhead = DataTrajectory.AllCars(MyAspect, ahead);

            if (carsAhead.Contains(closest)) { letIn = true; break; }

			if (closest.sensor.currentlyTurning) {
				letIn = true; break;
			}

            // If they have collided with something, break

            if (closest.RecentlyReset) { break; }



            // checks if the car is still in either lane adjacent to the participant


            Lane relevantLane;

            if (inNotOut) { relevantLane = sensor.firstLane.nextLaneIn; }
            else { relevantLane = sensor.firstLane.nextLaneOut; }

            bool otherCarStillAdjacent = DataTrajectory.AllCars(MyAspect,sensor.ProjectStraightAhead(150f, relevantLane)).Contains(closest);

            if (!otherCarStillAdjacent) { break; }

            yield return null;

        }


        bool carBehind = false;
        Aspect otherCollider;

        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(100f);
        if (DataTrajectory.AllCars(MyAspect, behind).Count > 0)
        {
            carBehind = true;
        }

        if (!CollisionHappened) { otherCollider = null; }
        else { otherCollider = ColliderAspect; }

        Fact fact = new Fact();

        fact.Add("Scenario Completed", "Cooperation_3");
        fact.Add("Scenario POV", gameObject+"");
        fact.Add("Ended with collision", CollisionHappened + "");

        if (otherCollider != null) { 
        fact.Add("Other collider", otherCollider.name + "");
        }
        else
        {
            fact.Add("Other collider", "nobody");

        }

        fact.Add("Other car", closest + "");
        fact.Add("Other car let in", letIn + "");
        fact.Add("Time taken", timeTaken + "");
        fact.Add("Minimum deceleration", minimumDeceleration + "");
        fact.Add("Car behind participant", "" + carBehind);
        fact.AddTimeStamp();

        //record that the scenario is over
        Scenarios.Remove("Cooperation 3");

        Log(fact);


    }

    public void CheckCooperationScenario3()
    {

        /* COOPERATION 3: Lane sharing
         * DESC: The participant is in dense traffic and an adjacent car signals to enter the participant's lane
         * START: Car is in adjacent lane between 0 and 100 metres in front of the participant
         * START: Car signals
         * END: The participant passes the other car OR the other car enters the participant's lane
         * MEASURES: Discrete: Does the participant allow the other to enter in front of them?
         * MEASURES: Continuous: How long between signal and entering the lane?
         * MEASURES: Continuous: Minimum deceleration?
         * CONTEXT: Is there a car behind (<100m) the participant?
         * CONTEXT: Identity of the other vehicle?
         */

        // if you're already involved in cooperation 3, return

        if (InvolvedInScenario("Cooperation 3") == true)
        {
            return;
        }

        if (sensor.currentlyTurning)
        {
            return;
        }

        float distLeft = 9999f, distRight = 9999f;
        Aspect closestLeft = null, closestRight = null;


        float distToProject = 100f;

        float roadNarrowsIn = sensor.distanceToNarrowing - 1f;

        distToProject = Mathf.Min(distToProject, roadNarrowsIn);


        // look to the left

        if (sensor.firstLane.lanesFromEdge > 0)
        {



            List<DataTrajectory> leftLane = sensor.ProjectStraightAhead(distToProject, sensor.firstLane.nextLaneOut);
            closestLeft = DataTrajectory.ClosestCar(MyAspect, leftLane, out distLeft);

            if (closestLeft != null)
            {
                if (!closestLeft.signal.signallingRight)
                {
                    closestLeft = null;
                }
            }

        }


        // look to the right

        if (sensor.firstLane.lanesFromCentre > 0)
        {

            List<DataTrajectory> rightLane = sensor.ProjectStraightAhead(distToProject, sensor.firstLane.nextLaneIn);
            closestRight = DataTrajectory.ClosestCar(MyAspect, rightLane, out distRight);

            if (closestRight != null)
            {
                if (!closestRight.signal.signallingLeft)
                {
                    closestRight = null;
                }
            }

        }

        if (closestLeft == null && closestRight == null) { return; }

        Aspect closest;
        bool inNotOut = false;

        if (distLeft < distRight)
        {

            inNotOut = true;
            closest = closestLeft;
        }
        else
        {
            closest = closestRight;
        }

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(sensor.distanceToNarrowing + 10f,sensor.firstLane);
        List<Aspect> carsAhead = DataTrajectory.AllCars(MyAspect, ahead);

        if (carsAhead.Contains(closest)) { return; }

        Debug.LogError(closest+":"+sensor.firstLane + ":"+sensor.distanceToNarrowing);

        StartCoroutine(Cooperation3(closest, inNotOut));

    }

    public void CheckCooperationScenario2()
    {
        /* COOPERATION 2: AMBIGUOUS INTERSECTION
               * DESCRIPTION: The driver is in dense traffic and enters an ambiguous intersection.
               * START: Participant in a 4-way stop
               * START: Other car within 20 metres
               * END: Participant gets through four-way stop
               * MEASURES: Discrete: Who goes first?
               * CONTEXT: Who should go first?
               * CONTEXT: How many vehicles are within 20m?
               * CONTEXT: Identity of other vehicles
               */

        // if you're already involved in a coop2, return

        if (InvolvedInScenario("Cooperation 2") == true)
        {
            return;
        }

        // return if you're not at a four-way stop

        Junction fourway = sensor.nextFourWayStop;

        if (fourway == null) { return; }

        List<Aspect> otherCars = fourway.CarsApproachingWithin(20f);

        // if you're not within 20m yourself of the four-way, return
        if (!otherCars.Contains(MyAspect)) { return; }

        // if only you are approaching the fourway, return

        if (otherCars.Count == 1) { return; }

        otherCars.Remove(MyAspect);

        Aspect closest = otherCars[0];
        float lowD = Vector3.Distance(closest.transform.position, fourway.position);

        foreach (Aspect a in otherCars)
        {
            if (Vector3.Distance(a.transform.position, fourway.transform.position) < lowD)
            {
                lowD = Vector3.Distance(a.transform.position, fourway.transform.position);
                closest = a;
            }
        }


		if (closest.RecentlyReset) {
			return;
		}

        StartCoroutine(Cooperation2(closest, fourway));


    }

    public void CheckCooperationScenario1()
    {

        /* COOPERATION 1: MERGING
         * DESC: The participant is in dense traffic which is narrowing down to one lane
         * DESC: The participant is in the 'lucky lane'
         * START: Within 200m of narrowing
         * START: Another car is in the 'unlucky lane' between 2 and 100 metres ahead of the participant
         * END: Past the intersection
         * MEASURES: Discrete - does the participant let the vehicle in?
         * MEASURES: Continuous: Driver acceleration rate
         * CONTEXT: Identity of the other vehicle
         */

        // If you're involved in Cooperation 1 already, return

        if (InvolvedInScenario("Cooperation 1") == true)
        {
            return;
        }

        // If you're more than 200m away from the lane narrowing, return
        if (sensor.distanceToNarrowing > 200f)
        {
            return;
        }

        // If you're on the edge of a road (and therefore in the 'unlucky' lane), return

        if (sensor.firstLane.lanesFromEdge == 0)
        {
            return;
        }

        // get the closest car in the next lane out

        Lane nextLaneOut = sensor.firstLane.nextLaneOut;
        
        List<DataTrajectory> ahead = sensor.ProjectStraightAhead(100f, nextLaneOut);

        float distAhead;

        Aspect closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);

        // if the closest car is nearer than 2m or further away than 100m, return

        if(distAhead<2f || distAhead > 100f)
        {
            return;
        }

        Debug.Log("NARROW: " + sensor.distanceToNarrowing + " vs " +closestAhead.sensor.distanceToNarrowing);


        Debug.Log("Starting cooperation 1");

        StartCoroutine(Cooperation1(closestAhead));

      


    }

    #endregion

    #region Communication Scenarios

    public IEnumerator Communication1(Aspect otherCar)
    {
       /*
        * END: Participant turns
        * END: OR participant passes the point where they were supposed to turn
        * MEASURE: Discrete: Does the participant signal?
        * MEASURE: Discrete: Does the rear car collide with participant?
        * MEASURE: Continuous: How far from the intersection does the participant signal?
        * MEASURE: Continuous: Minimum deceleration rate
        * CONTEXT: Is the turn into oncoming traffic(i.e.a right turn)
        * CONTEXT: Number of vehicles behind driver
        * CONTEXT: Identity of the other car
        * CONTEXT: Initial velocity of participant car
        */

        //record that the scenario is in progress
        RecordScenario("Communication 1");

        bool signalled = false;
        float signalDist = 0f;

        float minimumDeceleration = sensor.acceleration;

        Direction turnDirection = sensor.turnDirection;

        float initialVelocity = sensor.speed;

        int numberBehind = DataTrajectory.AllCars(MyAspect,sensor.ProjectStraightBehind(100f)).Count;

        while (true)
        {

            if (sensor.acceleration < minimumDeceleration)
            {
                minimumDeceleration = sensor.acceleration;
            }
       


            // MEASURE: Does the participant signal?

            if (MyAspect.signalling && !signalled) {

                   signalDist = sensor.distanceToTurn;

                    signalled = true;


                
            }


            // If collision happened, break out

            if (CollisionHappened) { break; }


            // If they have collided with something, break

            if (otherCar.RecentlyReset) { 

				Scenarios.Remove("Communication 1");
				yield break; 
			}

            // triggered when the point where the turn is supposed to begin has been passed

            if (sensor.currentlyTurning) { break; }

            yield return null;

        }


        Aspect otherCollider;

        if (!CollisionHappened) { otherCollider = null; }
        else { otherCollider = ColliderAspect; }

        Fact fact = new Fact();

        fact.Add("Scenario Completed", "Communication_1");
        fact.Add("Scenario POV", gameObject.name);
        fact.Add("Signalled", signalled + "");
        fact.Add("Signal Distance", signalDist + "");
        fact.Add("Ended with collision", CollisionHappened+"");

        if (otherCollider != null) { 
        fact.Add("Other collider", otherCollider + "");
        }
        else
        {
            fact.Add("Other collider", "nothing");

        }
        fact.Add("Other car", otherCar + "");
        fact.Add("Initial Velocity", initialVelocity + "");
        fact.Add("Min Deceleration Rate", minimumDeceleration + "");
        fact.Add("Turn direction", turnDirection + "");
        fact.Add("Cars behind participant", numberBehind + "");
        fact.AddTimeStamp();

        //record that the scenario is over
        Scenarios.Remove("Communication 1");

        Log(fact);

    }

    public void CheckCommunicationScenarios()
    {

        /* COMMUNICATION 1: LEAD THE FOLLOWER
         * DESC: Another vehicle is following the participant on a stretch of road
         * DESC: The participant must turn off the road
         * START TRIGGER: The participant is within 80m of a turn
         * START TRIGGER: A car is within 200m behind the car
         * END: Participant turns
         * END: OR participant passes the point where they were supposed to turn
         * MEASURE: Discrete: Does the participant signal?
         * MEASURE: Discrete: Does the rear car collide with participant?
         * MEASURE: Continuous: How far from the intersection does the participant signal?
         * MEASURE: Continuous: Minimum deceleration rate
         * CONTEXT: Is the turn into oncoming traffic (i.e. a right turn)
         * CONTEXT: Number of vehicles behind driver
         * CONTEXT: Identity of the other car
         * CONTEXT: Initial velocity of participant car
         */

        if (InvolvedInScenario("Communication 1") == true)
        {
            return;
        }

        if(sensor.distanceToTurn > 80f) { return; }

		if (sensor.currentlyTurning) {
			return;
		}

        Aspect closestBehind; float distBehind;

        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(200f);

        closestBehind = DataTrajectory.ClosestCar(MyAspect, behind, out distBehind);

        if (closestBehind == null) { return; }

		if (closestBehind.RecentlyReset) {
			return;
		}

        StartCoroutine(Communication1(closestBehind));

    }

    #endregion

    #region Attention Scenarios

    public void CheckAttentionScenarios()
    {

        /* ATTENTION 1 - FOLLOW THE LEADER
         * DESC: Participant is following another vehicle on a stretch of road
         * DESC: The other driver signals and slows down to turn
         * START: Another vehicle is immediately in front of the participant (<200m)
         * START: Vehicle signals to turn
         * END: Participant passes car
         * END: OR car turns
         * MEASURES: Discrete: Does the participant collide?
         * MEASURES: Continuous: How close does the participant get to the other vehicle?
         * Continuous: How much closer (relative to the beginning of scenario) does the participant get?
         * Continuous: What is the minimum rate of deceleration?
         * Context: How far from the turn does the other car signal?
         * Context: Minimum deceleration rate of other car?
         * Context: Is there a car within 200m behind the participant?
         * Context: Identity of other car
         */


        if (InvolvedInScenario("Attention 1") == true)
        {
            return;
        }

   //     if (sensor.inJunction) { return; }

        Aspect closestAhead; float distAhead;

        List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(200f);

        closestAhead = DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead);



        if(closestAhead == null) { return; }

        Debug.DrawLine(transform.position + Vector3.up + Vector3.up + Vector3.up, closestAhead.gameObject.transform.position, Color.yellow);


        if (distAhead > sensor.distanceToTurn) { return; }

        if (!closestAhead.signalling) { return; }

		if (closestAhead.sensor.currentlyTurning) {
			return;
		}

        StartCoroutine(Attention1(closestAhead));

    }

    public IEnumerator Attention1(Aspect closest)
    {



        //record that the scenario is in progress
        RecordScenario("Attention 1");

        float distAhead;

        float distFromTurnOtherSignal = -1f;

        float initialDistance = Vector3.Distance(closest.transform.position, this.transform.position);
        float distance = 0f;
        float minimumDeceleration = sensor.acceleration;

        while (true)
        {
            Debug.DrawLine(transform.position + Vector3.up + Vector3.up + Vector3.up, closest.gameObject.transform.position, Color.white);


            if (distFromTurnOtherSignal < 0f)
            {
                if (closest.signalling)
                {
                    distFromTurnOtherSignal = closest.sensor.distanceToTurn;
                }
            }

            if (sensor.acceleration < minimumDeceleration)
            {
                minimumDeceleration = sensor.acceleration;
            }


            distance = Vector3.Distance(closest.transform.position, this.transform.position);



            // If collision happened, break out

            if (CollisionHappened) { break; }


            // If they have collided with something, break

			if (closest.RecentlyReset) {         Scenarios.Remove("Attention 1");
				yield break; }

            List<DataTrajectory> ahead = MySensor.ProjectStraightAhead(200f);

            if(closest != DataTrajectory.ClosestCar(MyAspect, ahead, out distAhead)) { break; }

            yield return null;

        }


        bool carBehind = false;

        List<DataTrajectory> behind = MySensor.ProjectStraightBehind(200f);

        if (DataTrajectory.AllCars(MyAspect, behind).Count > 0)
        {
            carBehind = true;
        }


        Aspect otherCollider;

        if (!CollisionHappened) { otherCollider = null; }
        else { otherCollider = ColliderAspect; }

        Fact fact = new Fact();

        fact.Add("Scenario Completed", "Attention_1");
        fact.Add("Scenario POV", gameObject.name);
        fact.Add("Ended with collision", CollisionHappened + "");

        if (otherCollider != null) { 
        fact.Add("Other collider", otherCollider + "");
        }
        else
        {
            fact.Add("Other collider", "nothing");
        }

        fact.Add("Other car", closest + "");
        fact.Add("Distance", distance + "");
        fact.Add("Distance from turn other car signals", distFromTurnOtherSignal + "");
        fact.Add("Change in distance", (distance-initialDistance) + "");
        fact.Add("Min Deceleration Rate", minimumDeceleration + "");
        fact.Add("Car behind participant", "" + carBehind);
        
        fact.AddTimeStamp();

        //record that the scenario is over
        Scenarios.Remove("Attention 1");

        Log(fact);


    }


    #endregion

    #region Fact Logging

    public class Fact
    {

        public static int FACTID = 0;

        public List<string> info; public List<string> title; public int id;

        public int size
        {
            get
            {

                if (info.Count != title.Count)
                {
                    Debug.LogError("Error! Different amount of titles and information in Fact!");
                }
                return info.Count;
            }
        }

        public Fact()
        {
            id = FACTID;
            FACTID = FACTID + 1;

            info = new List<string>(); title = new List<string>();

            Add("SCENE", SceneSelector.ID);

        }

        public void Add(string _title, string _info)
        {
            title.Add(_title); info.Add(_info);
        }

        public void AddTimeStamp()
        {
            float f = Time.time;
            Add("time", f.ToString("n2"));
        }

    }

    public void Log(string title, string info)
    {

        Fact fact = new Fact();
        fact.Add(title, info);
        Log(fact);

    }

    public void Log(Fact f)
    {

        if (DebugTextUpdater.INSTANCE != null)
        {


            bool scenarioFlag = false;

           for(int i = 0; i<f.size; i++)
            {

                string check = f.title[i];

                if(check == "Scenario Completed")
                {
                    scenarioFlag = true;
                    ScenarioOverview.Record(f.info[i]);
                }

            }


            if (scenarioFlag) { 

            string s = "";
            for (int i = 0; i < f.size; i++)
            {
                s=s+(f.id + ":" + f.title[i] + " = " + f.info[i]);
                s = s + "\n";
            }

            DebugTextUpdater.INSTANCE.SetMostRecentScenario(s);
            }
        }

        if (LogTo == LoggingType.Console)
        {

            for (int i = 0; i < f.size; i++)
            {
                Debug.Log(f.id + ":" + f.title[i] + " = " + f.info[i]);
            }

        }

        if (LogTo == LoggingType.Cloud)
        {

            EventData e = new EventData();

            for (int i = 0; i < f.size; i++)
            {

                Information info = new Information(f.title[i], f.info[i]);

                e.Add(info);


            }

			if (Analytics.INSTANCE != null) {
				Analytics.LogEvent (e);
			} else {
				Debug.Log("fact unlogged due to no analytics");
			}
        }
    }

    #endregion

}
