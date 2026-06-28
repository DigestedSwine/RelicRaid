using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

// Minimal networked player for the Fusion Shared-Mode connection slice: the client that has state
// authority over this object reads WASD and moves it; NetworkTransform replicates it to everyone else.
// (First-pass demo movement — the full HeroController/prediction integration comes after sync is proven.)
public class NetPlayer : NetworkBehaviour
{
    public float speed = 5f;

    public override void Spawned()
    {
        // The local player drives the camera.
        if (HasStateAuthority)
        {
            var cam = Camera.main;
            if (cam != null) { var cf = cam.GetComponent<CameraFollow>(); if (cf != null) cf.target = transform; }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;          // only the owner moves it; others receive via NetworkTransform
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector3 dir = Vector3.zero;
        if (kb.wKey.isPressed) dir.z += 1f;
        if (kb.sKey.isPressed) dir.z -= 1f;
        if (kb.dKey.isPressed) dir.x += 1f;
        if (kb.aKey.isPressed) dir.x -= 1f;

        if (dir.sqrMagnitude > 0.01f)
        {
            dir.Normalize();
            transform.position += dir * speed * Runner.DeltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
