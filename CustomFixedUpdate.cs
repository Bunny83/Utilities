using UnityEngine;
using System.Collections;
 
public class CustomFixedUpdate
{
	public delegate void OnFixedUpdateCallback(float aDeltaTime);
	private float m_FixedTimeStep;
	private float m_Timer = 0;
 
	private OnFixedUpdateCallback m_Callback;
 
	private float m_MaxAllowedTimeStep = 0f;
	public float MaxAllowedTimeStep
	{
		get { return m_MaxAllowedTimeStep; }
		set { m_MaxAllowedTimeStep = value;}
	}
 
	public float deltaTime
	{
		get { return m_FixedTimeStep; }
		set	{m_FixedTimeStep = Mathf.Max (value, 0.000001f); } // max rate: 1000000
	}
	public float updateRate
	{
		get { return 1.0f / deltaTime; }
		set { deltaTime = 1.0f / value; }
	}
 
	public CustomFixedUpdate(float aTimeStep, OnFixedUpdateCallback aCallback, float aMaxAllowedTimestep)
	{
		if (aCallback == null)
			throw new System.ArgumentException("CustomFixedUpdate needs a valid callback");
		if (aTimeStep <= 0f)
			throw new System.ArgumentException("TimeStep needs to be greater than 0");
		deltaTime = aTimeStep;
		m_Callback = aCallback;
		m_MaxAllowedTimeStep = aMaxAllowedTimestep;
	}
	public CustomFixedUpdate(float aTimeStep, OnFixedUpdateCallback aCallback) : this(aTimeStep, aCallback, 0f) {}
	public CustomFixedUpdate(OnFixedUpdateCallback aCallback) : this(0.01f, aCallback, 0f) {}
	public CustomFixedUpdate(OnFixedUpdateCallback aCallback, float aFPS, float aMaxAllowedTimestep) : this(1f/aFPS, aCallback, aMaxAllowedTimestep){}
	public CustomFixedUpdate(OnFixedUpdateCallback aCallback, float aFPS) : this(aCallback, aFPS, 0f){}
 
 
	public void Update(float aDeltaTime)
	{
		m_Timer -= aDeltaTime;
		if (m_MaxAllowedTimeStep > 0)
		{
			float timeout = Time.realtimeSinceStartup + m_MaxAllowedTimeStep;
			while(m_Timer < 0f && Time.realtimeSinceStartup < timeout)
			{
				m_Callback(m_FixedTimeStep);
				m_Timer += m_FixedTimeStep;
			}
		}
		else
		{
			while(m_Timer < 0f)
			{
				m_Callback(m_FixedTimeStep);
				m_Timer += m_FixedTimeStep;
			}
		}
	}
 
	public void Update()
	{
		Update(Time.deltaTime);
	}
}
