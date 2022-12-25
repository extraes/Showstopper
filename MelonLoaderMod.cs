using BoneLib;
using Jevil;
using Jevil.Patching;
using Jevil.Prefs;
using MelonLoader;
using SLZ.Bonelab;
using SLZ.Interaction;
using SLZ.Marrow.Pool;
using SLZ.Marrow.SceneStreaming;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Showstopper;

public static class BuildInfo
{
    public const string Name = "Showstopper!"; // Name of the Mod.  (MUST BE SET)
    public const string Author = "extraes"; // Author of the Mod.  (Set as null if none)
    public const string Company = null; // Company that made the Mod.  (Set as null if none)
    public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
    public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
}

[Preferences("Showstopper!", false)]
public class Showstopper : MelonMod
{
    public Showstopper() : base() => instance = this;
    internal static Showstopper instance;
    static UnityObjectComparer<Rigidbody> rbComparer = new();

    static Dictionary<Rigidbody, RigidbodyState> states = new();
    static bool freezing;
    static bool frozen;

    //[Pref("If true: unfreezes using rigidbodies found from its grip; If false: unfreezes using rigidbodies found from poolee check")]
    //static bool findUnfreezeBodiesViaGrip = true;
    [Pref] static bool unfreezeOnGrab = true;
    [Pref] static UnfreezeFindMode unfreezeFindMode = UnfreezeFindMode.AUTO;

    public override void OnInitializeMelon()
    {
        Preferences.Register<Showstopper>();
        //Hook.OntoMethod(typeof(Hand).GetMethod(nameof(Hand.AttachObject)), Hooking_OnGrabObject);
        //Hooking.OnGripAttached += (grip, hand) => Hooking_OnGrabObject(grip.gameObject, hand);
        Hooking.OnGrabObject += Hooking_OnGrabObject;
    }

    public override void OnFixedUpdate()
    {
        if (!Player.handsExist) return;

        if (Player.rightController.GetThumbStickDown() && Time.timeScale < 0.2f) 
            ToggleFreeze();
    }

    [Pref]
    static void ToggleFreeze()
    {
#if DEBUG
        Log($"{nameof(ToggleFreeze)} called, {nameof(freezing)}={freezing}, {nameof(frozen)}={frozen}");
#endif
        if (freezing) return;
        freezing = true;

        if (frozen) Unfreeze();
        else Freeze();
    }
    
    static void Freeze()
    {
        MelonCoroutines.Start(CoFreeze());
    }

    static IEnumerator CoFreeze()
    {
        states.Clear();
        // IsChildOf(Instances.Player_RigManager.transform)
        Rigidbody[] bodies = GameObject.FindObjectsOfType<Rigidbody>().Where(rb => !rb.transform.InHierarchyOf(Const.RigManagerName)).ToArray();
#if DEBUG
        Log($"{nameof(CoFreeze)} found {bodies.Length} bodies.");
#endif

        yield return null;

        foreach (Rigidbody body in bodies)
        {
            states[body] = new(body);
        }
        
        yield return null;

        foreach (Rigidbody body in bodies)
        {
            body.isKinematic = true;
        }

        yield return null;

        try
        {
            if (Player.leftHand.HasAttachedObject()) Hooking_OnGrabObject(Player.leftHand.m_CurrentAttachedGO, Player.leftHand);
            if (Player.rightHand.HasAttachedObject()) Hooking_OnGrabObject(Player.rightHand.m_CurrentAttachedGO, Player.rightHand);
            //Player.leftHand.m_CurrentAttachedGO?.GetComponentsInChildren<Rigidbody>().ForEach(RestoreRigidbody);
            //Player.rightHand.m_CurrentAttachedGO?.GetComponentsInChildren<Rigidbody>().ForEach(RestoreRigidbody);
        } 
        catch (Exception ex)
        {
            Error(ex);
        }

        // crate barcode for bonelab hub
        if (SceneStreamer.Session.Level.Barcode.ID == "c2534c5a-6b79-40ec-8e98-e58c5363656e")
        {
            foreach (HubDoorController hdc in GameObject.FindObjectsOfType<HubDoorController>())
            {
                try
                {
                    RestoreRigidbody(hdc.rb);
                }
                catch(Exception ex)
                {
#if DEBUG
                    Warn("BONELAB Hub door controller: " + ex);
#endif
                }
            }
            yield return null;
        }

        freezing = false;
        frozen = true;
#if DEBUG
        Log($"{nameof(CoFreeze)} finished execution");
#endif
    }

    static void Unfreeze()
    {
        MelonCoroutines.Start(CoUnfreeze());
    }

    static IEnumerator CoUnfreeze()
    {
        foreach (var kvp in states)
        {
            try
            {
                if (kvp.Key.INOC()) throw new NullReferenceException("collected object");
                kvp.Value.ApplyTo(kvp.Key);
                //kvp.Key.isKinematic = kvp.Value;
            }
            catch (Exception ex)
            {
#if DEBUG
                Warn(ex);
#endif
            }
        }

        yield return null;

#if DEBUG
        Log($"{nameof(Unfreeze)} has restored {states.Count} objects");
#endif
        yield return null;

        frozen = false;
        freezing = false;
        states.Clear();
    }

    static void Hooking_OnGrabObject(GameObject grabbedObj, SLZ.Interaction.Hand hand)
    {
#if DEBUG
        Log($"{nameof(Hooking_OnGrabObject)} has been called. {nameof(grabbedObj)} = {grabbedObj.transform.GetFullPath()}, hand = {hand?.handedness ?? SLZ.Handedness.UNDEFINED}");
        Log($"{nameof(Hooking_OnGrabObject)} UFOG={unfreezeOnGrab} ");
        //hand.GetGripInHand()
#endif

        if (!unfreezeOnGrab) return;
        Rigidbody[] rbs = GetRigidbodies(grabbedObj);

#if DEBUG
        Log($"H_OGO: got {rbs.Length} rigidbodies");
        foreach (var rb in rbs)
            Log($" - {rb.transform.GetFullPath()}; kinem={rb.isKinematic}");
#endif
        foreach (Rigidbody body in rbs)
        {
            body.isKinematic = false;
            if (states.ContainsKey(body)) states.Remove(body);
            //RestoreRigidbody(body);
        }
#if DEBUG
        Log($"H_OGO: postcheck on {rbs.Length} rigidbodies");
        foreach (var rb in rbs)
            Log($" - {rb.transform.GetFullPath()}; kinem={rb.isKinematic}");
#endif
    }

    static Rigidbody[] GetRigidbodies(GameObject grabbedObj)
    {
        switch (unfreezeFindMode)
        {
            default:
            case UnfreezeFindMode.AUTO:
                Rigidbody[] rbsPoolee = BodiesFromPoolee(grabbedObj);
                if (rbsPoolee.Length == 0)
                    return BodiesFromGrip(grabbedObj);
                else return rbsPoolee;
            case UnfreezeFindMode.GRIP:
                return BodiesFromGrip(grabbedObj);
            case UnfreezeFindMode.POOLEE:
                return BodiesFromPoolee(grabbedObj);
        }
    }

    static Rigidbody[] BodiesFromPoolee(GameObject obj)
    {
        GameObject poolee = obj.GetComponentInParent<AssetPoolee>()?.gameObject;

        if (poolee != null)
            return poolee.GetComponentsInChildren<Rigidbody>();
        else return obj.GetComponentsInChildren<Rigidbody>();
    }

    static Rigidbody[] BodiesFromGrip(GameObject obj)
    {
        Grip g = Grip.Cache.Get(obj);
        return g.gripColliders.Select(g => g.attachedRigidbody).NoNull().Distinct(rbComparer).ToArray();
    }

    static void RestoreRigidbody(Rigidbody rb)
    {
        if (states.TryGetValue(rb, out RigidbodyState state)) state.ApplyTo(rb);
    }

    #region MelonLogger replacements

    internal static void Log(string str) => instance.LoggerInstance.Msg(str);
    internal static void Log(object obj) => instance.LoggerInstance.Msg(obj?.ToString() ?? "null");
    internal static void Warn(string str) => instance.LoggerInstance.Warning(str);
    internal static void Warn(object obj) => instance.LoggerInstance.Warning(obj?.ToString() ?? "null");
    internal static void Error(string str) => instance.LoggerInstance.Error(str);
    internal static void Error(object obj) => instance.LoggerInstance.Error(obj?.ToString() ?? "null");

    #endregion
}
