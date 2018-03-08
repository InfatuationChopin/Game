using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LogicFrameMove : MonoBehaviour
{
	// 目标位置
	private VInt3 targetPosition;
	// 预测的位置
//	private VInt3 forecastPosition;
	// 人物的移动速度
	private VInt speedNormal;
	// 当前帧的人物移动的真实速度
	private VInt speedReal;
	// 移动状态
	private MoveStatus moveStatus;
	// 方向向量
	private VInt3 dirVector; 

	// 移动动画相关
	private Animation animation;
	private string idleAnimationName;
	private string moveAnimationName;
	private bool isNormalAnimation = false;

	public VInt3 position {
		get{ return targetPosition; }
		set{ targetPosition = value; }
	}
	public MoveStatus status {
		get{ return moveStatus; }
	}
	public VInt3 dir {
		get{ return dirVector; }
	}

	void Start ()
	{
		targetPosition = (VInt3)transform.position;
		dirVector = (VInt3)transform.forward;
//		forecastPosition = VInt3.zero;
		moveStatus = MoveStatus.Stop;
	}

	public void StartMove (VInt3 moveVector, VInt speed)
	{
		// 保存方向向量
		dirVector = moveVector;
		// 计算下一步应该要走的位置
		targetPosition = targetPosition + moveVector * LogicFrame.frameIntervalTime * speed;
		// 计算移动速度
		speedNormal = speed;
		speedReal = VInt3.Distance ((VInt3)transform.position, targetPosition) / (LogicFrame.frameIntervalTime + (VInt)Time.deltaTime / (VInt)2.0f);
		if (moveStatus == MoveStatus.Forecast) {
			Debug.Log ("嘻嘻 => " + speedReal.scalar);
		}
		// 移动的状态
		moveStatus = MoveStatus.Target;
	}

	public void StopMove ()
	{
		VInt3 currPosition = (VInt3)transform.position;
		if (moveStatus == MoveStatus.Target || moveStatus == MoveStatus.Forecast) {
			if (currPosition != targetPosition) {
				moveStatus = MoveStatus.Back;
				speedReal = VInt3.Distance (currPosition, targetPosition) / (LogicFrame.frameIntervalTime + (VInt)Time.deltaTime / (VInt)2.0f);
			} else {
				moveStatus = MoveStatus.Stop;
			}
		}
	}

	void Update ()
	{
		if (moveStatus == MoveStatus.Target) {
			transform.LookAt (transform.position + (Vector3)dirVector);
			transform.position = Vector3.MoveTowards (transform.position, (Vector3)targetPosition, ((VInt)Time.deltaTime * speedReal).scalar);
			if ((VInt3)transform.position == targetPosition) {
				moveStatus = MoveStatus.Forecast;
			}
		} else if (moveStatus == MoveStatus.Forecast) {
			transform.position += (Vector3)((VInt3)transform.forward * (VInt)Time.deltaTime * speedNormal);
		} else if (moveStatus == MoveStatus.Back) {
			transform.position = Vector3.MoveTowards (transform.position, (Vector3)targetPosition, ((VInt)Time.deltaTime * speedReal).scalar);
			if (transform.position == (Vector3)targetPosition) {
				moveStatus = MoveStatus.Stop;
			}
		}

		// 移动动画
		MoveAnimation ();
	}

	void MoveAnimation ()
	{
		if (animation == null) {
			return;
		}
		if (moveStatus == MoveStatus.Target || moveStatus == MoveStatus.Forecast) {
			isNormalAnimation = true;
			animation.Play (moveAnimationName);
		} else {
			if (isNormalAnimation) {
				animation.Play (idleAnimationName);
				isNormalAnimation = false;
			}
		}
	}

	public void SetAnimation (Animation animation, string idleAnimationName, string moveAnimationName)
	{
		this.animation = animation;
		this.idleAnimationName = idleAnimationName;
		this.moveAnimationName = moveAnimationName;
	}

	public enum MoveStatus
	{
		Stop,
		Target,
		Forecast,
		Back
	}

}
