using BoneLib.BoneMenu.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Showstopper;

public struct RigidbodyState
{
    public bool kinematic;
    public Vector3 vel;
    public Vector3 angVel;

    public RigidbodyState(Rigidbody rb)
    {
        kinematic = rb.isKinematic;
        vel = rb.velocity;
        angVel = rb.angularVelocity;
    }

    public void ApplyTo(Rigidbody rb)
    {
        rb.isKinematic = kinematic;
        rb.velocity = vel;
        rb.angularVelocity = angVel;
    }
}
