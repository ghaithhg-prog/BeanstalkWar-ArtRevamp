using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// CharacterVisualAnimator:
/// Drives real Mixamo AnimationClips via Unity's PlayableGraph for Erika Archer (Idle, Walk, Run, Jump, Draw+Recoil, Death),
/// and provides procedural skeletal locomotion for custom non-clip rigs (Medieval Warrior).
/// </summary>
public class CharacterVisualAnimator : MonoBehaviour
{
    [Header("Bone References (Auto-detected)")]
    public Transform hips;
    public Transform spine;
    public Transform chest;
    public Transform head;

    public Transform leftUpLeg;
    public Transform leftLeg;
    public Transform leftFoot;

    public Transform rightUpLeg;
    public Transform rightLeg;
    public Transform rightFoot;

    public Transform leftArm;
    public Transform leftForeArm;
    public Transform rightArm;
    public Transform rightForeArm;

    [Header("Animation Tuning")]
    public float strideSpeed = 9.5f;
    public float legAngleRange = 36.0f;
    public float armAngleRange = 32.0f;
    public float bobAmount = 0.045f;
    public float breathingSpeed = 2.2f;

    [Header("Preview / Showcase Mode")]
    public bool isPreviewShowcase = false;

    // Base initial rotations
    private Quaternion initHipsRot;
    private Quaternion initSpineRot;
    private Quaternion initChestRot;
    private Quaternion initHeadRot;

    private Quaternion initLeftUpLegRot;
    private Quaternion initLeftLegRot;
    private Quaternion initRightUpLegRot;
    private Quaternion initRightLegRot;

    private Quaternion initLeftArmRot;
    private Quaternion initLeftForeArmRot;
    private Quaternion initRightArmRot;
    private Quaternion initRightForeArmRot;

    private Vector3 initHipsPos;

    // State tracking
    private Rigidbody parentRb;
    private UnityEngine.AI.NavMeshAgent parentAgent;
    private PlayerMovement playerMovement;
    private CharacterCombat characterCombat;
    private Health parentHealth;
    private BotController parentBot;

    private float walkCycle = 0f;
    private float moveBlend = 0f;
    private float attackGestureTimer = 0f;
    private float attackGestureDuration = 0.72f;
    private float jumpGestureTimer = 0f;
    private bool isAttacking = false;
    private bool isDeadState = false;

    private bool hasBow = false;

    // PlayableGraph fields for real Mixamo animations (Erika Archer)
    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable playIdle;
    private AnimationClipPlayable playWalk;
    private AnimationClipPlayable playRun;
    private AnimationClipPlayable playJump;
    private AnimationClipPlayable playDraw;
    private AnimationClipPlayable playRecoil;
    private AnimationClipPlayable playDeath;

    private AnimationClip clipIdle;
    private AnimationClip clipWalk;
    private AnimationClip clipRun;
    private AnimationClip clipJump;
    private AnimationClip clipDraw;
    private AnimationClip clipRecoil;
    private AnimationClip clipShoot;
    private AnimationClip clipDeath;

    private bool usePlayableGraph = false;
    private readonly float[] slotWeights = new float[7];

    public bool UsesRealDeathAnimation => usePlayableGraph && clipDeath != null;

    void Start()
    {
        FindBones();
        CaptureInitialPose();

        Transform rootTarget = transform.parent != null ? transform.parent : transform;
        parentRb = rootTarget.GetComponent<Rigidbody>();
        parentAgent = rootTarget.GetComponent<UnityEngine.AI.NavMeshAgent>();
        playerMovement = rootTarget.GetComponent<PlayerMovement>();
        characterCombat = rootTarget.GetComponent<CharacterCombat>();
        parentHealth = rootTarget.GetComponent<Health>();
        parentBot = rootTarget.GetComponent<BotController>();

        // التحقق مما إذا كانت هذه الشخصية هي رامية السهام (Erika Archer / Akai)
        hasBow = gameObject.name.ToLower().Contains("erika") ||
                 gameObject.name.ToLower().Contains("akai") ||
                 (transform.parent != null && transform.parent.name.ToLower().Contains("archer"));
        if (!hasBow)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string tn = t.name.ToLower();
                if (tn.Contains("bow") || tn.Contains("erika"))
                {
                    hasBow = true;
                    break;
                }
            }
        }

        if (hasBow)
        {
            SetupErikaPlayableGraph();
        }
    }

    private AnimationClip LoadFirstValidClip(string resourcePath)
    {
        AnimationClip[] arr = Resources.LoadAll<AnimationClip>(resourcePath);
        if (arr != null)
        {
            foreach (AnimationClip c in arr)
            {
                if (c != null && !c.name.ToLower().Contains("__preview__"))
                    return c;
            }
        }
        return Resources.Load<AnimationClip>(resourcePath);
    }

    private void SetupErikaPlayableGraph()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = gameObject.AddComponent<Animator>();

        anim.enabled = true;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("Models/Erika_Archer");
        if (clips == null || clips.Length < 2)
        {
            clips = Resources.LoadAll<AnimationClip>("Models/Akai");
        }

        if (clips != null)
        {
            foreach (AnimationClip c in clips)
            {
                if (c == null) continue;
                string cn = c.name.ToLower();
                if (cn.Contains("__preview__")) continue;

                if (cn.Contains("idle") && clipIdle == null) clipIdle = c;
                else if (cn.Contains("walk") && clipWalk == null) clipWalk = c;
                else if (cn.Contains("run") && clipRun == null) clipRun = c;
                else if (cn.Contains("jump") && clipJump == null) clipJump = c;
                else if (cn.Contains("recoil") && clipRecoil == null) clipRecoil = c;
                else if ((cn.Contains("shoot") || cn.Contains("overdraw")) && clipShoot == null) clipShoot = c;
                else if (cn.Contains("draw") && clipDraw == null) clipDraw = c;
                else if (cn.Contains("death") && clipDeath == null) clipDeath = c;
            }
        }

        if (clipIdle == null) clipIdle = LoadFirstValidClip("Models/Erika_Idle");
        if (clipWalk == null) clipWalk = LoadFirstValidClip("Models/Erika_Walk");
        if (clipRun == null) clipRun = LoadFirstValidClip("Models/Erika_Run");
        if (clipJump == null) clipJump = LoadFirstValidClip("Models/Erika_Jump");
        if (clipDraw == null) clipDraw = LoadFirstValidClip("Models/Erika_Draw");
        if (clipRecoil == null) clipRecoil = LoadFirstValidClip("Models/Erika_Recoil");
        if (clipShoot == null) clipShoot = LoadFirstValidClip("Models/Erika_Shoot");
        if (clipDeath == null) clipDeath = LoadFirstValidClip("Models/Erika_Death");

        if (clipWalk == null) clipWalk = clipRun;
        if (clipRun == null) clipRun = clipWalk;
        if (clipRecoil == null) clipRecoil = clipShoot != null ? clipShoot : clipDraw;

        if (clipIdle == null && clipRun == null)
        {
            return;
        }

        playableGraph = PlayableGraph.Create("ErikaArcherGraph_" + gameObject.GetInstanceID());
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(playableGraph, "Animation", anim);
        mixer = AnimationMixerPlayable.Create(playableGraph, 7);
        output.SetSourcePlayable(mixer);

        if (clipIdle != null)
        {
            playIdle = AnimationClipPlayable.Create(playableGraph, clipIdle);
            playIdle.SetSpeed(1.0f);
            playableGraph.Connect(playIdle, 0, mixer, 0);
        }
        if (clipWalk != null)
        {
            playWalk = AnimationClipPlayable.Create(playableGraph, clipWalk);
            playWalk.SetSpeed(1.05f);
            playableGraph.Connect(playWalk, 0, mixer, 1);
        }
        if (clipRun != null)
        {
            playRun = AnimationClipPlayable.Create(playableGraph, clipRun);
            playRun.SetSpeed(1.45f);
            playableGraph.Connect(playRun, 0, mixer, 2);
        }
        if (clipJump != null)
        {
            playJump = AnimationClipPlayable.Create(playableGraph, clipJump);
            playableGraph.Connect(playJump, 0, mixer, 3);
        }
        if (clipDraw != null)
        {
            playDraw = AnimationClipPlayable.Create(playableGraph, clipDraw);
            playableGraph.Connect(playDraw, 0, mixer, 4);
        }
        if (clipRecoil != null)
        {
            playRecoil = AnimationClipPlayable.Create(playableGraph, clipRecoil);
            playableGraph.Connect(playRecoil, 0, mixer, 5);
        }
        if (clipDeath != null)
        {
            playDeath = AnimationClipPlayable.Create(playableGraph, clipDeath);
            playableGraph.Connect(playDeath, 0, mixer, 6);
        }

        slotWeights[0] = 1f;
        for (int i = 0; i < 7; i++)
        {
            mixer.SetInputWeight(i, slotWeights[i]);
        }

        playableGraph.Play();
        usePlayableGraph = true;
    }

    void FindBones()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms)
        {
            string n = t.name.ToLower();

            // Hips / Pelvis
            if (hips == null && (n.Contains("hips") || n.Contains("pelvis"))) hips = t;

            // Spine / Chest / Head
            else if (spine == null && n.Contains("spine") && !n.Contains("spine1") && !n.Contains("spine2")) spine = t;
            else if (chest == null && (n.Contains("chest") || n.Contains("spine2") || n.Contains("spine1") || n.Contains("torso"))) chest = t;
            else if (head == null && (n.Contains("head") || n.Contains("neck")) && !n.Contains("top")) head = t;

            // Left Leg
            else if (leftUpLeg == null && (n.Contains("leftupleg") || (n.Contains("upper_leg") && n.Contains(".l")) || n.Contains("thigh.l"))) leftUpLeg = t;
            else if (leftLeg == null && (n.Contains("leftleg") || (n.Contains("lower_leg") && n.Contains(".l")) || n.Contains("shin.l") || n.Contains("calf.l"))) leftLeg = t;
            else if (leftFoot == null && (n.Contains("leftfoot") || (n.Contains("foot") && n.Contains(".l")))) leftFoot = t;

            // Right Leg
            else if (rightUpLeg == null && (n.Contains("rightupleg") || (n.Contains("upper_leg") && n.Contains(".r")) || n.Contains("thigh.r"))) rightUpLeg = t;
            else if (rightLeg == null && (n.Contains("rightleg") || (n.Contains("lower_leg") && n.Contains(".r")) || n.Contains("shin.r") || n.Contains("calf.r"))) rightLeg = t;
            else if (rightFoot == null && (n.Contains("rightfoot") || (n.Contains("foot") && n.Contains(".r")))) rightFoot = t;

            // Left Arm
            else if (leftArm == null && (n.Contains("leftarm") || (n.Contains("upper_arm") && n.Contains(".l")) || (n.Contains("arm") && n.Contains(".l") && !n.Contains("fore")))) leftArm = t;
            else if (leftForeArm == null && (n.Contains("leftforearm") || (n.Contains("lower_arm") && n.Contains(".l")) || n.Contains("forearm.l"))) leftForeArm = t;

            // Right Arm
            else if (rightArm == null && (n.Contains("rightarm") || (n.Contains("upper_arm") && n.Contains(".r")) || (n.Contains("arm") && n.Contains(".r") && !n.Contains("fore")))) rightArm = t;
            else if (rightForeArm == null && (n.Contains("rightforearm") || (n.Contains("lower_arm") && n.Contains(".r")) || n.Contains("forearm.r"))) rightForeArm = t;
        }

        if (hips != null) initHipsPos = hips.localPosition;
    }

    void CaptureInitialPose()
    {
        if (hips != null) initHipsRot = hips.localRotation;
        if (spine != null) initSpineRot = spine.localRotation;
        if (chest != null) initChestRot = chest.localRotation;
        if (head != null) initHeadRot = head.localRotation;

        if (leftUpLeg != null) initLeftUpLegRot = leftUpLeg.localRotation;
        if (leftLeg != null) initLeftLegRot = leftLeg.localRotation;
        if (rightUpLeg != null) initRightUpLegRot = rightUpLeg.localRotation;
        if (rightLeg != null) initRightLegRot = rightLeg.localRotation;

        if (leftArm != null) initLeftArmRot = leftArm.localRotation;
        if (leftForeArm != null) initLeftForeArmRot = leftForeArm.localRotation;
        if (rightArm != null) initRightArmRot = rightArm.localRotation;
        if (rightForeArm != null) initRightForeArmRot = rightForeArm.localRotation;
    }

    void LateUpdate()
    {
        // 1. Calculate movement speed
        float currentSpeed = 0f;
        if (parentRb != null && !parentRb.isKinematic)
        {
            Vector3 horizVel = new Vector3(parentRb.linearVelocity.x, 0f, parentRb.linearVelocity.z);
            currentSpeed = horizVel.magnitude;
        }
        else if (parentAgent != null && parentAgent.enabled)
        {
            currentSpeed = parentAgent.velocity.magnitude;
        }
        else if (parentBot != null && parentBot.IsGlidingWithCrystal)
        {
            currentSpeed = 12.0f;
        }

        bool isLocalHuman = (parentBot == null) && (playerMovement == null || playerMovement.IsOwner);
        bool isSprinting = false;

        if (isLocalHuman)
        {
            isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || PlayerMovement.MobileSprintActive || PlayerMovement.SprintToggleActive;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if ((Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f) && currentSpeed < 0.5f)
            {
                currentSpeed = isSprinting ? 10.0f : 6.0f;
            }
        }
        else
        {
            float runThreshold = (playerMovement != null) ? (playerMovement.walkSpeed + 1.5f) : 7.5f;
            isSprinting = currentSpeed >= runThreshold;
        }

        if (attackGestureTimer > 0f)
        {
            attackGestureTimer -= Time.deltaTime;
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }

        if (jumpGestureTimer > 0f)
        {
            jumpGestureTimer -= Time.deltaTime;
        }

        // إذا كانت الشخصية تستخدم PlayableGraph (مثل Erika Archer)، نشغل أنيميشن Mixamo الحقيقي 100% دون أي تدوير إجرائي للعظام
        if (usePlayableGraph && playableGraph.IsValid())
        {
            UpdatePlayableGraph(currentSpeed, isSprinting);
            return;
        }

        if (hips == null) return;

        bool isMoving = currentSpeed > 0.35f;
        float targetBlend = isMoving ? 1f : 0f;
        moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, Time.deltaTime * 6.5f);

        // 3. Update Procedural Locomotion for non-clip characters
        if (isMoving)
        {
            float sprintMult = isSprinting ? 1.55f : 1.0f;
            walkCycle += Time.deltaTime * strideSpeed * sprintMult * Mathf.Clamp(currentSpeed / 5f, 0.6f, 1.8f);
        }

        AnimateLegsAndBody(currentSpeed);
        AnimateArmsAndUpperBody();
    }

    private void WrapLoopingPlayable(AnimationClipPlayable playable, AnimationClip clip)
    {
        if (!playable.IsValid() || clip == null || clip.length <= 0.05f) return;
        double t = playable.GetTime();
        if (t >= clip.length)
        {
            playable.SetTime(t % clip.length);
        }
    }

    private void UpdatePlayableGraph(float currentSpeed, bool isSprinting)
    {
        // تكرار سلس لحركات الوقوف والمشي والجري
        WrapLoopingPlayable(playIdle, clipIdle);
        WrapLoopingPlayable(playWalk, clipWalk);
        WrapLoopingPlayable(playRun, clipRun);

        // التحقق من حالة الموت والعودة للحياة
        bool healthDead = (parentHealth != null && parentHealth.isDead);
        if (healthDead && !isDeadState)
        {
            TriggerDeathAnimation();
        }
        else if (!healthDead && isDeadState && parentHealth != null)
        {
            ResetAfterRespawn();
        }

        int activeSlot = 0; // 0:Idle, 1:Walk, 2:Run, 3:Jump, 4:Draw, 5:Recoil, 6:Death
        float blendRate = 12f;

        if (isDeadState)
        {
            activeSlot = 6;
            blendRate = 16f;
            if (playDeath.IsValid() && clipDeath != null)
            {
                double dt = playDeath.GetTime();
                double maxTime = Mathf.Max(0.05f, clipDeath.length - 0.04f);
                if (dt >= maxTime)
                {
                    playDeath.SetTime(maxTime);
                    playDeath.SetSpeed(0f);
                }
            }
        }
        else if (isAttacking)
        {
            blendRate = 18f;
            float elapsed = attackGestureDuration - attackGestureTimer;
            if (elapsed < 0.38f && playDraw.IsValid())
            {
                activeSlot = 4; // سحب السهم ورفع القوس (Standing Draw Arrow)
            }
            else if (playRecoil.IsValid())
            {
                activeSlot = 5; // إطلاق السهم وارتداد الوتر (Standing Aim Recoil)
            }
            else
            {
                activeSlot = 4;
            }
        }
        else
        {
            bool airborne = jumpGestureTimer > 0f;

            if (airborne && playJump.IsValid() && clipJump != null)
            {
                activeSlot = 3; // القفز المتزامن مع الصعود الفيزيائي في نفس اللحظة
                blendRate = 22f;
                double jt = playJump.GetTime();
                // تجميد الحركة عند ذروة القفزة الهوائية قبل الهبوط لمنع تكرار القفز في الهواء
                double maxJumpTime = Mathf.Clamp(clipJump.length * 0.62f, 0.48f, Mathf.Max(0.50f, clipJump.length - 0.15f));
                if (jt >= maxJumpTime)
                {
                    playJump.SetTime(maxJumpTime);
                }
            }
            else if (currentSpeed > 0.35f)
            {
                if (isSprinting && playRun.IsValid())
                {
                    activeSlot = 2; // الجري السريع بالشفت (Standing Run Forward)
                    playRun.SetSpeed(1.45f);
                    blendRate = 16f;
                }
                else if (playWalk.IsValid())
                {
                    activeSlot = 1; // المشي العادي بدون شفت (Walking)
                    playWalk.SetSpeed(1.05f);
                }
                else if (playRun.IsValid())
                {
                    activeSlot = 2;
                }
            }
            else
            {
                activeSlot = 0; // الوقوف الطبيعي مع القوس (Standing Idle)
            }
        }

        float weightSum = 0f;
        for (int i = 0; i < 7; i++)
        {
            float targetW = (i == activeSlot) ? 1f : 0f;
            slotWeights[i] = Mathf.MoveTowards(slotWeights[i], targetW, Time.deltaTime * blendRate);
            weightSum += slotWeights[i];
        }

        if (weightSum > 0.0001f)
        {
            for (int i = 0; i < 7; i++)
            {
                mixer.SetInputWeight(i, slotWeights[i] / weightSum);
            }
        }

        // تثبيت الحوض (Hips) في مكانه المحلي أثناء القفز والحركة حتى لا يرتفع المجسم مرة ثانية في الهواء فوق ارتفاع الفيزياء!
        if (!isDeadState && hips != null && initHipsPos.sqrMagnitude > 0.0001f)
        {
            hips.localPosition = initHipsPos;
        }
    }

    void AnimateLegsAndBody(float speed)
    {
        float legSin = Mathf.Sin(walkCycle);
        float legCos = Mathf.Cos(walkCycle);

        float idleTime = Time.time;
        float breath = Mathf.Sin(idleTime * breathingSpeed);
        float slowSway = Mathf.Sin(idleTime * 0.7f);

        // Pelvis bobbing & weight transfer: natural breathing bob + weight shift in idle
        float moveBob = Mathf.Abs(legSin) * bobAmount * moveBlend;
        float idleBob = (Mathf.Abs(breath) * 0.016f - 0.008f) * (1f - moveBlend);
        float idleSwayX = slowSway * 0.014f * (1f - moveBlend);
        hips.localPosition = initHipsPos + new Vector3(idleSwayX, -moveBob + idleBob, 0f);

        // Hips rotation: subtle weight shift between hips
        if (moveBlend < 0.95f)
        {
            Quaternion idleHipsRot = initHipsRot * Quaternion.Euler(0f, slowSway * 2.5f, slowSway * 2.0f);
            hips.localRotation = Quaternion.Slerp(initHipsRot, idleHipsRot, 1f - moveBlend);
        }
        else
        {
            hips.localRotation = initHipsRot;
        }

        // Spine lean into motion + living idle posture
        if (spine != null)
        {
            float leanAngle = Mathf.Clamp(speed * 2.2f, 0f, 14f) * moveBlend;
            float moveSpineZ = legSin * 2.5f * moveBlend;
            float idleSpineX = (1.5f + breath * 1.2f) * (1f - moveBlend);
            float idleSpineY = (-slowSway * 2.0f) * (1f - moveBlend);
            float idleSpineZ = (-slowSway * 1.5f) * (1f - moveBlend);
            spine.localRotation = initSpineRot * Quaternion.Euler(leanAngle + idleSpineX, idleSpineY, moveSpineZ + idleSpineZ);
        }

        // Chest counter-twist + deep warrior chest breathing
        if (chest != null)
        {
            float chestBreath = (-3.8f * Mathf.Clamp01(breath * 0.5f + 0.5f)) * (1f - moveBlend);
            float moveChestZ = legSin * 3f * moveBlend;
            float idleChestY = (slowSway * 1.5f) * (1f - moveBlend);
            chest.localRotation = initChestRot * Quaternion.Euler(chestBreath, idleChestY, moveChestZ);
        }

        // Head looking around & scanning attentiveness
        if (head != null)
        {
            float headScan = Mathf.Sin(idleTime * 0.55f) * 7.5f * (1f - moveBlend);
            float headPitch = (Mathf.Cos(idleTime * 0.75f) * 2.5f - 1.0f) * (1f - moveBlend);
            head.localRotation = initHeadRot * Quaternion.Euler(headPitch, headScan, headScan * 0.15f);
        }

        bool isMixamo = (hips != null && hips.name.ToLower().Contains("mixamo")) || hasBow;

        if (isMixamo)
        {
            // لشخصيات Mixamo: محور حركة الفخذين للأمام والخلف هو Z
            float legAngle = legSin * (legAngleRange * 0.7f) * moveBlend;
            if (leftUpLeg != null)
            {
                leftUpLeg.localRotation = initLeftUpLegRot * Quaternion.Euler(0f, 0f, legAngle);
            }
            if (rightUpLeg != null)
            {
                rightUpLeg.localRotation = initRightUpLegRot * Quaternion.Euler(0f, 0f, -legAngle);
            }
            if (leftLeg != null)
            {
                leftLeg.localRotation = initLeftLegRot * Quaternion.Euler(0f, 0f, Mathf.Clamp(-legAngle * 0.8f, 0f, 35f));
            }
            if (rightLeg != null)
            {
                rightLeg.localRotation = initRightLegRot * Quaternion.Euler(0f, 0f, Mathf.Clamp(legAngle * 0.8f, 0f, 35f));
            }
        }
        else
        {
            // Upper legs (Thighs): Natural grounded warrior stance when idle, stride when moving
            float idleLeftThigh = (8.0f + slowSway * 1.5f) * (1f - moveBlend);
            float idleRightThigh = (-4.0f - slowSway * 1.5f) * (1f - moveBlend);
            float moveLeftThigh = legSin * legAngleRange * moveBlend;
            float moveRightThigh = -legSin * legAngleRange * moveBlend;

            if (leftUpLeg != null)
            {
                leftUpLeg.localRotation = initLeftUpLegRot * Quaternion.Euler(moveLeftThigh + idleLeftThigh, 0f, -3.0f * (1f - moveBlend));
            }

            if (rightUpLeg != null)
            {
                rightUpLeg.localRotation = initRightUpLegRot * Quaternion.Euler(moveRightThigh + idleRightThigh, 0f, 3.0f * (1f - moveBlend));
            }

            // Lower legs (Knees bend backwards on back swing, and relaxed slight bend in idle)
            float idleLeftKnee = -12.0f * (1f - moveBlend);
            float idleRightKnee = 5.0f * (1f - moveBlend);
            float moveLeftKnee = Mathf.Clamp(-legSin * legAngleRange * 1.1f, 0f, 50f) * moveBlend;
            float moveRightKnee = Mathf.Clamp(legSin * legAngleRange * 1.1f, 0f, 50f) * moveBlend;

            if (leftLeg != null)
            {
                leftLeg.localRotation = initLeftLegRot * Quaternion.Euler(moveLeftKnee + idleLeftKnee, 0f, 0f);
            }

            if (rightLeg != null)
            {
                rightLeg.localRotation = initRightLegRot * Quaternion.Euler(moveRightKnee + idleRightKnee, 0f, 0f);
            }
        }
    }

    void AnimateArmsAndUpperBody()
    {
        // إذا كانت الشخصية رامي سهام (Akai / Archer)
        if (hasBow)
        {
            if (isAttacking)
            {
                // وضعية التصويب وإطلاق السهم (Frame 75):
                // اليد اليسرى تمسك القوس وترفعه موجهاً مباشرة نحو الهدف
                // اليد اليمنى تسحب وتر القوس إلى جانب الوجه مع ثني الساعد
                Quaternion aimLeftArm = initLeftArmRot * Quaternion.Euler(17.8f, -48.6f, 20.3f);
                Quaternion aimRightArm = initRightArmRot * Quaternion.Euler(-23.3f, 32.2f, -80.2f);
                Quaternion aimRightFore = initRightForeArmRot * Quaternion.Euler(0f, 0f, -107.9f);

                if (leftArm != null)
                {
                    leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, aimLeftArm, Time.deltaTime * 18f);
                }
                if (rightArm != null)
                {
                    rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, aimRightArm, Time.deltaTime * 18f);
                }
                if (rightForeArm != null)
                {
                    rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, aimRightFore, Time.deltaTime * 18f);
                }
                if (leftForeArm != null)
                {
                    leftForeArm.localRotation = Quaternion.Slerp(leftForeArm.localRotation, initLeftForeArmRot, Time.deltaTime * 18f);
                }
            }
            else
            {
                // وضعية الوقوف والاسترخاء الطبيعي وحمل القوس (Frame 1):
                // تنزل اليدين بنعومة من وضعية T-pose إلى جانبي الجسد عند الورك
                float breathe = Mathf.Sin(Time.time * breathingSpeed) * 2.0f;
                Quaternion idleLeftArm = initLeftArmRot * Quaternion.Euler(68.75f + breathe, -7.57f, -13.50f);
                Quaternion idleRightArm = initRightArmRot * Quaternion.Euler(54.06f + breathe, -9.35f, -12.62f);
                Quaternion idleRightFore = initRightForeArmRot * Quaternion.Euler(0f, 0f, -39.36f);

                if (leftArm != null)
                {
                    leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, idleLeftArm, Time.deltaTime * 10f);
                }
                if (rightArm != null)
                {
                    rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, idleRightArm, Time.deltaTime * 10f);
                }
                if (rightForeArm != null)
                {
                    rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, idleRightFore, Time.deltaTime * 10f);
                }
                if (leftForeArm != null)
                {
                    leftForeArm.localRotation = Quaternion.Slerp(leftForeArm.localRotation, initLeftForeArmRot, Time.deltaTime * 10f);
                }
            }
            return;
        }

        if (isAttacking)
        {
            // ضربة سيف قوية للمحاربين وبقية الأبطال
            if (rightArm != null)
            {
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, initRightArmRot * Quaternion.Euler(-65f, 35f, -40f), Time.deltaTime * 15f);
            }
            if (leftArm != null)
            {
                leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, initLeftArmRot * Quaternion.Euler(-20f, 0f, 25f), Time.deltaTime * 10f);
            }
            return;
        }

        // Preview Showcase flourish gesture every ~8.5 seconds (Ready salute)
        float flourishLift = 0f;
        if (isPreviewShowcase)
        {
            float fCycle = Time.time % 8.5f;
            if (fCycle < 1.4f)
            {
                flourishLift = Mathf.Sin((fCycle / 1.4f) * Mathf.PI) * 22f;
            }
        }

        float idleTimeNow = Time.time;
        float breathSine = Mathf.Sin(idleTimeNow * breathingSpeed);
        float stdArmSin = Mathf.Sin(walkCycle);

        // Left Arm: Defensive Guard & Shield Posture when idle, walking swing when moving
        if (leftArm != null)
        {
            Quaternion idleLeftArm = initLeftArmRot * Quaternion.Euler(56f + breathSine * 2.2f, -18f, 28f);
            Quaternion moveLeftArm = initLeftArmRot * Quaternion.Euler(-stdArmSin * armAngleRange, 0f, 3f);
            Quaternion targetLeft = Quaternion.Slerp(idleLeftArm, moveLeftArm, moveBlend);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetLeft, Time.deltaTime * 10f);
        }

        // Left ForeArm: Bent in sturdy guard posture when idle
        if (leftForeArm != null)
        {
            Quaternion idleLeftFore = initLeftForeArmRot * Quaternion.Euler(-55f + breathSine * 1.5f, 0f, 16f);
            float runBend = Mathf.Clamp(stdArmSin * 15f, 0f, 25f);
            Quaternion moveLeftFore = initLeftForeArmRot * Quaternion.Euler(-12f - runBend, 0f, 0f);
            Quaternion targetFore = Quaternion.Slerp(idleLeftFore, moveLeftFore, moveBlend);
            leftForeArm.localRotation = Quaternion.Slerp(leftForeArm.localRotation, targetFore, Time.deltaTime * 10f);
        }

        // Right Arm: Weapon Ready guard + subtle challenge flourish when idle
        if (rightArm != null)
        {
            Quaternion idleRightArm = initRightArmRot * Quaternion.Euler(50f + breathSine * 2.2f - flourishLift, 16f, -22f);
            Quaternion moveRightArm = initRightArmRot * Quaternion.Euler(stdArmSin * armAngleRange, 0f, -3f);
            Quaternion targetRight = Quaternion.Slerp(idleRightArm, moveRightArm, moveBlend);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRight, Time.deltaTime * 10f);
        }

        // Right ForeArm: Forearm angled ready to strike when idle
        if (rightForeArm != null)
        {
            Quaternion idleRightFore = initRightForeArmRot * Quaternion.Euler(-50f + breathSine * 1.5f - flourishLift * 0.4f, 0f, -12f);
            float runBend = Mathf.Clamp(-stdArmSin * 15f, 0f, 25f);
            Quaternion moveRightFore = initRightForeArmRot * Quaternion.Euler(-12f - runBend, 0f, 0f);
            Quaternion targetRightFore = Quaternion.Slerp(idleRightFore, moveRightFore, moveBlend);
            rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, targetRightFore, Time.deltaTime * 10f);
        }
    }

    public void TriggerAttackGesture()
    {
        if (isDeadState) return;
        attackGestureDuration = 0.72f;
        attackGestureTimer = attackGestureDuration;

        if (usePlayableGraph && playableGraph.IsValid())
        {
            if (playDraw.IsValid())
            {
                playDraw.SetTime(0);
                playDraw.SetSpeed(1.85f);
            }
            if (playRecoil.IsValid())
            {
                playRecoil.SetTime(0);
                playRecoil.SetSpeed(1.35f);
            }
        }
    }

    private float lastJumpGestureTime = -10f;

    public void TriggerJumpGesture()
    {
        if (isDeadState || (Time.time - lastJumpGestureTime < 0.12f)) return;
        lastJumpGestureTime = Time.time;
        jumpGestureTimer = 0.55f;

        if (usePlayableGraph && playableGraph.IsValid() && playJump.IsValid())
        {
            // البدء مباشرة من لحظة الانطلاق في الهواء (تخطي الانحناءة البطيئة على الأرض) ليتطابق القفز مع الصعود في نفس اللحظة!
            double startFrameTime = (clipJump != null && clipJump.length > 0.6f) ? 0.32 : 0.0;
            playJump.SetTime(startFrameTime);
            playJump.SetSpeed(1.1f);
            slotWeights[3] = 0.85f;
        }
    }

    public void TriggerDeathAnimation()
    {
        isDeadState = true;
        attackGestureTimer = 0f;
        jumpGestureTimer = 0f;

        if (usePlayableGraph && playableGraph.IsValid() && playDeath.IsValid())
        {
            playDeath.SetTime(0);
            playDeath.SetSpeed(1.0f);
        }
    }

    public void ResetAfterRespawn()
    {
        isDeadState = false;
        attackGestureTimer = 0f;
        jumpGestureTimer = 0f;

        if (usePlayableGraph && playableGraph.IsValid())
        {
            if (playIdle.IsValid()) playIdle.SetTime(0);
            if (playDeath.IsValid())
            {
                playDeath.SetTime(0);
                playDeath.SetSpeed(1.0f);
            }
            for (int i = 0; i < 7; i++)
            {
                slotWeights[i] = (i == 0) ? 1f : 0f;
                mixer.SetInputWeight(i, slotWeights[i]);
            }
        }
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}
