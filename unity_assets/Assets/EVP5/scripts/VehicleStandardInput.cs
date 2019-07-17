//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;

namespace EVP
{

public class VehicleStandardInput : NetworkBehaviour
	{
	public VehicleController target;

	public bool continuousForwardAndReverse = true;

	public enum ThrottleAndBrakeInput { SingleAxis, SeparateAxes };
	public ThrottleAndBrakeInput throttleAndBrakeInput = ThrottleAndBrakeInput.SingleAxis;
    
	public string steerAxis = "Horizontal";
	public string throttleAndBrakeAxis = "Vertical";
	public string throttleAxis = "Fire2";
	public string brakeAxis = "Fire3";
	public string handbrakeAxis = "Jump";
	public KeyCode resetKey = KeyCode.Return;

	bool m_doReset = false;


        void OnEnable()
        {


            // throttleAndBrakeAxis = ;


            if (target == null)
            {
                target = GetComponent<VehicleController>();

            }
        }


        void Update ()
		{



            if (!isLocalPlayer)
            {
                return;
            }

            if (target == null) return;

            if (Input.GetKeyDown(resetKey)) {

                Debug.Log("REBOOTING BECAUSE RESET KEY PRESSED");

                GetComponent<NetworkCarController>().Reboot();
            }
                
		}


	void FixedUpdate ()
	{

		if (!isLocalPlayer)
		{
			return;
		}

		if (target == null) return;

		// Read the user input

		float steerInput = 0.0f;
		float handbrakeInput = 0.0f;
		
		float throttleInput = 0.5f; 
		float brakeInput = 0.0f;
		
		string message = null;
		ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
		using (RequestSocket client = new RequestSocket())
		{ 
			client.Connect("tcp://localhost:5555");
			client.SendFrame("act ");
			bool gotMessage = false;
			while (!gotMessage)
			{
				gotMessage = client.TryReceiveFrameString(out message);
			}

			string[] inpt = message.Split(';');

			steerInput = float.Parse(inpt[0].Replace('.', ','));
			throttleInput = float.Parse(inpt[1].Replace('.', ','));
			brakeInput = float.Parse(inpt[2].Replace('.', ','));

			//if (gotMessage) System.IO.File.WriteAllText(@"C:\Users\vanoi_000\Documents\unity1816\Social Driving AI\Output\LOGGER"+Time.time.ToString()+".txt", message);
		}

		NetMQConfig.Cleanup();

		/* COMMENTED READ PLAYER INPUT LINES
		float steerInput = Mathf.Clamp(Input.GetAxis(steerAxis), -1.0f, 1.0f);
		float handbrakeInput = Mathf.Clamp01(Input.GetAxis(handbrakeAxis));


		float forwardInput = 0.0f;
		float reverseInput = 0.0f;

		if (throttleAndBrakeInput == ThrottleAndBrakeInput.SeparateAxes)
		{
			forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAxis));
			reverseInput = Mathf.Clamp01(Input.GetAxis(brakeAxis));
		}
		else 
		{ 
			forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAndBrakeAxis));
			reverseInput = Mathf.Clamp01(-Input.GetAxis(throttleAndBrakeAxis));
		}

		forwardInput += Mathf.Clamp01(Input.GetAxis("Pedals"));
		reverseInput += Mathf.Clamp01(-Input.GetAxis("Pedals"));


            // Translate forward/reverse to vehicle input
            
		float throttleInput = 0.0f; 
		float brakeInput = 0.0f;

		if (continuousForwardAndReverse)
		{
			float minSpeed = 0.1f;
			float minInput = 0.1f;

			if (target.speed > minSpeed)
			{
				throttleInput = forwardInput;
				brakeInput = reverseInput;
			}
			else
			{
				if (reverseInput > minInput)
				{
					throttleInput = -reverseInput;
					brakeInput = 0.0f;
				}
				else
				if (forwardInput > minInput)
				{
					if (target.speed < -minSpeed)
					{
						throttleInput = 0.0f;
						brakeInput = forwardInput;
					}
					else
					{
						throttleInput = forwardInput;
						brakeInput = 0;
					}
				}
			}
		}
		else
		{
			bool reverse = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

			if (!reverse)
			{
				throttleInput = forwardInput;
				brakeInput = reverseInput;
			}
			else
			{
				throttleInput = 0f;

				//-reverseInput;
				brakeInput = 0;
			}
		}
		*/ //END OF COMMENTED READ PLAYER INPUT LINES

            // Apply input to vehicle

            //if (target.speed > 0.1f)
            //target.steerInput = Mathf.Clamp(target.speedAngle / target.maxSteerAngle * 0.5f + steerInput, -1.0f, +1.0f);
            //else

            if (steerInput == 0f)
            {
                target.steerInput = 0f;
            }
            target.steerInput = steerInput;
                
                //* 0.75f;

            //                Mathf.Lerp(target.steerInput, steerInput, 0.03f);
            //     Debug.Log(target.steerInput);
            //target.steerInput = steerInput;

            //    Debug.Log(target.throttleInput);
            target.throttleInput = throttleInput;
            target.throttleInput = Mathf.Clamp(throttleInput,0f,1f);
		target.brakeInput = brakeInput;
		target.handbrakeInput = handbrakeInput;

            // Do a vehicle reset

            if (logWait > 0.2f) {
                logWait = 0f;
                Logger.Fact f = new Logger.Fact();
            f.Add("Steer", steerInput + "");
            f.Add("Throttle", throttleInput + "");
            f.Add("Brake", brakeInput + "");
            f.AddTimeStamp();
            Logger.INSTANCE.Log(f);
            }

            logWait += Time.fixedDeltaTime;
        }
        float logWait = 0f;

    }

}