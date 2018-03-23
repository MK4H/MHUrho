using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using MHUrho.Helpers;

namespace MHUrho.Logic
{
    public class Projectile : Component {
        private Map map;

        private Vector3 movement;

        private readonly float baseTimeToDespawn;
        private float timeToDespawn;

        public Projectile(Vector3 movement, Map map) {
            ReceiveSceneUpdates = true;
            this.map = map;
            this.movement = movement;
            this.baseTimeToDespawn = 6;
            this.timeToDespawn = baseTimeToDespawn;
        }

       
        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);
            
            if (map.IsInside(Node.Position)) {
                Node.Position += movement * timeStep;
                Node.LookAt(Node.Position + movement, Vector3.UnitY);

                movement += (-Vector3.UnitY * 10) * timeStep;
                timeToDespawn = baseTimeToDespawn;
            }
            else {
                //Stop movement
                movement = Vector3.Zero;
                timeToDespawn -= timeStep;
                if (timeToDespawn < 0) {
                    Node.Remove();
                }
            }
        }

    }
}
