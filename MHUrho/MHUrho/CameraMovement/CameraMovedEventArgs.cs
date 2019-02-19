using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.CameraMovement
{
	public enum CameraMode {
		RTS,
		FreeFloating,
		Following
	}
    public struct CameraMovedEventArgs
    {
		public Vector3 WorldPosition { get; private set; }
		public Quaternion WorldRotation { get; private set; }
		public CameraMode CameraMode { get; private set; }
		public IEntity FollowedEntity { get; private set; }

		public CameraMovedEventArgs(Vector3 worldPosition,
									Quaternion worldRotation,
									CameraMode cameraMode,
									IEntity followedEntity)
		{
			this.WorldPosition = worldPosition;
			this.WorldRotation = worldRotation;
			this.CameraMode = cameraMode;
			this.FollowedEntity = followedEntity;
		}

    }
}
